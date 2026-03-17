using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Frends.RabbitMQ.Read.Definitions;
using RabbitMQ.Client;

namespace Frends.RabbitMQ.Read;

/// <summary>
/// RabbitMQ Read task.
/// </summary>
public class RabbitMQ
{
    /// <summary>
    /// Read message(s) from RabbitMQ queue. Message data is byte[] encoded to base64 and UTF8 strings.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.RabbitMQ.Read)
    /// </summary>
    /// <param name="connection">Connection parameters.</param>
    /// <returns>Object { bool Success, Object { string Data, Dictionary&lt;string, string&gt; Headers, uint MessagesCount, ulong DeliveryTag } MessagesBase64, Object { string Data, Dictionary&lt;string, string&gt; Headers, uint MessagesCount, ulong DeliveryTag } MessageUTF8 }</returns>
    public static async Task<Result> Read([PropertyTab] Connection connection)
    {
        using var connectionHelper = new ConnectionHelper();
        var baseList = new List<Message>();
        var stringList = new List<Message>();

        await OpenConnectionIfClosed(connectionHelper, connection);

        while (connection.ReadMessageCount-- > 0)
        {
            var rcvMessage = await connectionHelper.AMQPModel.BasicGetAsync(queue: connection.QueueName, autoAck: connection.AckType == AckType.AutoAck);
            if (rcvMessage != null)
            {
                baseList.Add(new Message
                {
                    Data = Convert.ToBase64String(rcvMessage.Body.ToArray()),
                    Headers = GetResponseHeaderDictionary(rcvMessage.BasicProperties),
                    MessagesCount = rcvMessage.MessageCount,
                    DeliveryTag = rcvMessage.DeliveryTag
                });

                stringList.Add(new Message
                {
                    Data = Encoding.UTF8.GetString(rcvMessage.Body.ToArray()),
                    Headers = GetResponseHeaderDictionary(rcvMessage.BasicProperties),
                    MessagesCount = rcvMessage.MessageCount,
                    DeliveryTag = rcvMessage.DeliveryTag
                });
            }
            else
                break;
        }

        // Acking logic:
        // - AutoAck is handled when IChannel.BasicGetAsync() is called with autoAck: true.
        // - NoAck does not send AckMessage.
        // - Other types are handled in AcknowledgeMessage() method
        if (connection.AckType is AckType.AutoAck or AckType.NoAck)
            return new Result(true, baseList, stringList);
        foreach (var message in baseList)
            await AcknowledgeMessage(connection.AckType, message.DeliveryTag, connectionHelper);

        return new Result(true, baseList, stringList);
    }

    private static async Task AcknowledgeMessage(AckType ackType, ulong deliveryTag,
        ConnectionHelper connectionHelper)
    {
        if (connectionHelper == null || connectionHelper.AMQPModel.IsClosed)
            throw new Exception("No connection to RabbitMQ");

        switch (ackType)
        {
            case AckType.AutoNack:
                await connectionHelper.AMQPModel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
                break;

            case AckType.AutoNackAndRequeue:
                await connectionHelper.AMQPModel.BasicNackAsync(deliveryTag, multiple: false, requeue: true);
                break;

            case AckType.AutoReject:
                await connectionHelper.AMQPModel.BasicRejectAsync(deliveryTag, requeue: false);
                break;

            case AckType.AutoRejectAndRequeue:
                await connectionHelper.AMQPModel.BasicRejectAsync(deliveryTag, requeue: true);
                break;
            case AckType.NoAck:
            case AckType.AutoAck:
            default:
                throw new ArgumentException($"AcknowledgeMessage should not be called with {ackType}.", nameof(ackType));
        }
    }

    private static Dictionary<string, string> GetResponseHeaderDictionary(IReadOnlyBasicProperties basicProperties)
    {
        if (basicProperties == null) return null;

        var allHeaders = new Dictionary<string, string>()
            {
                { "HEADER_APPID",             basicProperties.AppId != null ? basicProperties.AppId : null },
                { "HEADER_CLUSTERID",         basicProperties.ClusterId != null ? basicProperties.ClusterId : null },
                { "HEADER_CONTENTENCODING",   basicProperties.ContentEncoding != null ? basicProperties.ContentEncoding : null },
                { "HEADER_CONTENTTYPE",       basicProperties.ContentType != null ? basicProperties.ContentType : null },
                { "HEADER_CORRELATIONID",     basicProperties.CorrelationId != null ? basicProperties.CorrelationId : null },
                { "HEADER_EXPIRATION",        basicProperties.Expiration != null ? basicProperties.Expiration : null},
                { "HEADER_MESSAGEID",         basicProperties.MessageId != null ? basicProperties.MessageId : null }
            }
        .Where(h => h.Value != null)
        .ToDictionary(h => h.Key, h => h.Value);

        if (basicProperties.IsHeadersPresent())
            foreach (var header in basicProperties.Headers.ToList())
            {
                if (header.Value.GetType() == typeof(byte[]))
                    allHeaders[header.Key] = Encoding.UTF8.GetString(header.Value as byte[]);
                else
                    allHeaders[header.Key] = header.Value.ToString();
            }

        return allHeaders;
    }

    internal static async Task OpenConnectionIfClosed(ConnectionHelper connectionHelper, Connection connection)
    {
        // Close connection if hostname has changed.
        if (IsConnectionHostNameChanged(connectionHelper, connection))
            await connectionHelper.AMQPModel.CloseAsync();

        if (connectionHelper.AMQPConnection == null || connectionHelper.AMQPConnection.IsOpen == false)
        {
            var factory = new ConnectionFactory();
            X509Certificate2 certToDispose = null;

            try
            {
                switch (connection.AuthenticationMethod)
                {
                    case AuthenticationMethod.URI:
                        factory.Uri = new Uri(connection.Host);
                        break;
                    case AuthenticationMethod.Host:
                        if (!string.IsNullOrWhiteSpace(connection.Username) || !string.IsNullOrWhiteSpace(connection.Password))
                        {
                            factory.UserName = connection.Username;
                            factory.Password = connection.Password;
                        }
                        factory.HostName = connection.Host;

                        if (connection.Port != 0) factory.Port = connection.Port;

                        break;
                    case AuthenticationMethod.Certificate:
                        factory.HostName = connection.Host;
                        if (connection.Port != 0)
                            factory.Port = connection.Port;
                        factory.Ssl.Enabled = true;
                        factory.Ssl.ServerName = connection.Host;
                        factory.Ssl.Version = connection.SslProtocol switch
                        {
                            SslProtocol.Tls12 => SslProtocols.Tls12,
                            SslProtocol.Tls13 => SslProtocols.Tls13,
                            _ => SslProtocols.None,
                        };
                        certToDispose = connection.CertificateSource switch
                        {
                            CertificateSource.File => new X509Certificate2(connection.ClientCertificatePath,
                                connection.ClientCertificatePassword),
                            CertificateSource.Base64 => new X509Certificate2(
                                Convert.FromBase64String(connection.CertificateBase64), connection.ClientCertificatePassword),
                            CertificateSource.RawBytes => new X509Certificate2(connection.CertificateBytes,
                                connection.ClientCertificatePassword),
                            CertificateSource.Store => LoadFromStore(connection.StoreThumbprint,
                                connection.CertificateStoreLocation),
                            _ => throw new InvalidEnumArgumentException("Unknown certificate source.")
                        };
                        factory.Ssl.Certs = new X509Certificate2Collection(certToDispose);
                        factory.AuthMechanisms = new IAuthMechanismFactory[]
                        {
                            new ExternalMechanismFactory()
                        };

                        break;
                    case AuthenticationMethod.CertificateWithCredentials:
                        factory.HostName = connection.Host;
                        if (connection.Port != 0)
                            factory.Port = connection.Port;
                        factory.UserName = connection.Username;
                        factory.Password = connection.Password;
                        factory.Ssl.Enabled = true;
                        factory.Ssl.ServerName = connection.Host;
                        factory.Ssl.Version = connection.SslProtocol switch
                        {
                            SslProtocol.Tls12 => SslProtocols.Tls12,
                            SslProtocol.Tls13 => SslProtocols.Tls13,
                            _ => SslProtocols.None,
                        };
                        certToDispose = connection.CertificateSource switch
                        {
                            CertificateSource.File => new X509Certificate2(connection.ClientCertificatePath,
                                connection.ClientCertificatePassword),
                            CertificateSource.Base64 => new X509Certificate2(
                                Convert.FromBase64String(connection.CertificateBase64), connection.ClientCertificatePassword),
                            CertificateSource.RawBytes => new X509Certificate2(connection.CertificateBytes,
                                connection.ClientCertificatePassword),
                            CertificateSource.Store => LoadFromStore(connection.StoreThumbprint,
                                connection.CertificateStoreLocation),
                            _ => throw new InvalidEnumArgumentException("Unknown certificate source.")
                        };
                        factory.Ssl.Certs = new X509Certificate2Collection(certToDispose);
                        break;
                }

                if (connection.AuthenticationMethod != AuthenticationMethod.URI && !string.IsNullOrWhiteSpace(connection.VirtualHost))
                    factory.VirtualHost = connection.VirtualHost;

                if (connection.Timeout != 0) factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(connection.Timeout);

                connectionHelper.AMQPConnection = await factory.CreateConnectionAsync();
            }
            finally
            {
                certToDispose?.Dispose();
            }
        }

        if (connectionHelper.AMQPModel == null || connectionHelper.AMQPModel.IsClosed)
            connectionHelper.AMQPModel = await connectionHelper.AMQPConnection.CreateChannelAsync();
    }

    internal static bool IsConnectionHostNameChanged(ConnectionHelper connectionHelper, Connection connection)
    {
        // If no current connection, host name is not changed
        if (connectionHelper.AMQPConnection == null || connectionHelper.AMQPConnection.IsOpen == false)
            return false;

        switch (connection.AuthenticationMethod)
        {
            case AuthenticationMethod.URI:
                var newUri = new Uri(connection.Host);
                return (connectionHelper.AMQPConnection.Endpoint.HostName != newUri.Host);
            case AuthenticationMethod.Host:
            case AuthenticationMethod.Certificate:
            case AuthenticationMethod.CertificateWithCredentials:
                return (connectionHelper.AMQPConnection.Endpoint.HostName != connection.Host);
            default:
                throw new ArgumentException($"IsConnectionHostNameChanged: AuthenticationMethod missing.");
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Unable to setup store on GitHub")]
    private static X509Certificate2 LoadFromStore(string thumbprint, CertificateStoreLocation location)
    {
        var storeLocation = location switch
        {
            CertificateStoreLocation.LocalMachine => StoreLocation.LocalMachine,
            _ => StoreLocation.CurrentUser,
        };

        using var store = new X509Store(StoreName.My, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var cert = store.Certificates
            .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
            .OfType<X509Certificate2>()
            .FirstOrDefault();

        if (cert == null)
            throw new Exception($"Certificate with thumbprint {thumbprint} not found.");

        return cert;
    }
}
