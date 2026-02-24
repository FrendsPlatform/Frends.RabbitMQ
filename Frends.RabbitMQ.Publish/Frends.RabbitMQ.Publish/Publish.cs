using Frends.RabbitMQ.Publish.Definitions;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Frends.RabbitMQ.Publish;

/// <summary>
/// RabbitMQ publish task.
/// </summary>
public static class RabbitMQ
{
    private static readonly ObjectCache RabbitMqConnectionCache = MemoryCache.Default;


    [ExcludeFromCodeCoverage]
    private static void RemovedCallback(CacheEntryRemovedArguments arg)
    {
        if (arg.CacheItem.Value is IConnection conn)
        {
            _ = Task.Run(async () =>
            {
                await conn.CloseAsync();
                conn.Dispose();
            });
        }
    }

    /// <summary>
    /// Publish message to RabbitMQ queue in UTF8 or byte array format.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.RabbitMQ.Publish)
    /// </summary>
    /// <param name="input">Input parameters</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="cancellationToken">CancellationToken given by Frends to terminate the Task.</param>
    /// <returns>Object { bool Success, string DataFormat, string DataString, byte[] DataByteArray, Dictionary&lt;string, string&gt; Headers }</returns>
    public static async Task<Result> Publish([PropertyTab] Input input, [PropertyTab] Connection connection,
        CancellationToken cancellationToken)
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
                    if (connection.Port != 0)
                        factory.Port = connection.Port;

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

            if (connection.Timeout != 0)
                factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(connection.Timeout);

            await using var channel = await GetRabbitMQChannel(connection, factory, cancellationToken);

            var dataType = input.InputType.Equals(InputType.ByteArray) ? "ByteArray" : "String";
            var data = input.InputType.Equals(InputType.ByteArray)
                ? input.DataByteArray
                : Encoding.UTF8.GetBytes(input.DataString);

            if (data.Length == 0)
                throw new ArgumentException("Publish: Message data is missing.");

            if (connection.Create)
            {
                // Create args dictionary for quorum queue arguments
                var args = new Dictionary<string, object>
                {
                    {
                        "x-queue-type", "quorum"
                    }
                };

                var queueInfo = await channel.QueueDeclareAsync(queue: connection.QueueName,
                    durable: connection.Durable,
                    exclusive: false,
                    autoDelete: connection.AutoDelete,
                    arguments: connection.Quorum ? args : null,
                    cancellationToken: cancellationToken);

                if (!string.IsNullOrEmpty(connection.ExchangeName))
                {
                    await channel.QueueBindAsync(queue: queueInfo.QueueName,
                        exchange: connection.ExchangeName,
                        routingKey: connection.RoutingKey,
                        arguments: null,
                        cancellationToken: cancellationToken);
                }
            }

            BasicProperties basicProperties = new()
            {
                Persistent = connection.Durable
            };
            AddHeadersToBasicProperties(basicProperties, input.Headers);

            var headers = new Dictionary<string, string>();

            if (basicProperties.Headers != null)
                foreach (var head in basicProperties.Headers)
                    headers.Add(head.Key.ToString(), head.Value.ToString());

            await channel.BasicPublishAsync(exchange: connection.ExchangeName,
                routingKey: connection.RoutingKey,
                mandatory: true,
                basicProperties: basicProperties,
                body: data,
                cancellationToken: cancellationToken);

            if (connection.ConnectionExpirationSeconds == 0)
            {
                var cacheKey = GenerateCacheKey(connection);
                RabbitMqConnectionCache.Remove(cacheKey);
            }

            return new Result(true, dataType,
                !string.IsNullOrEmpty(input.DataString) ? input.DataString : Encoding.UTF8.GetString(input.DataByteArray),
                input.DataByteArray ?? Encoding.UTF8.GetBytes(input.DataString),
                headers);
        }
        finally
        {
            certToDispose?.Dispose();
        }
    }

    private static void AddHeadersToBasicProperties(IBasicProperties basicProperties, Header[] headers)
    {
        if (headers == null) return;

        var messageHeaders = new Dictionary<string, object>();

        headers.ToList().ForEach(header =>
        {
            switch (header.Name.ToUpper())
            {
                case "APPID":
                case "HEADER_APPID":
                case "HEADER.APPID":
                    basicProperties.AppId = header.Value;

                    break;

                case "CLUSTERID":
                case "HEADER_CLUSTERID":
                case "HEADER.CLUSTERID":
                    basicProperties.ClusterId = header.Value;

                    break;

                case "CONTENTENCODING":
                case "HEADER_CONTENTENCODING":
                case "HEADER.CONTENTENCODING":
                    basicProperties.ContentEncoding = header.Value;

                    break;

                case "CONTENTTYPE":
                case "HEADER_CONTENTTYPE":
                case "HEADER.CONTENTTYPE":
                    basicProperties.ContentType = header.Value;

                    break;

                case "CORRELATIONID":
                case "HEADER_CORRELATIONID":
                case "HEADER.CORRELATIONID":
                    basicProperties.CorrelationId = header.Value;

                    break;

                case "EXPIRATION":
                case "HEADER_EXPIRATION":
                case "HEADER.EXPIRATION":
                    basicProperties.Expiration = header.Value;

                    break;

                case "MESSAGEID":
                case "HEADER_MESSAGEID":
                case "HEADER.MESSAGEID":
                    basicProperties.MessageId = header.Value;

                    break;

                default:
                    messageHeaders.Add(header.Name, header.Value);

                    break;
            }
        });

        if (messageHeaders.Any())
            basicProperties.Headers = messageHeaders;
    }

    private static async Task<IChannel> GetRabbitMQChannel(Connection connection, ConnectionFactory factory,
        CancellationToken cancellationToken)
    {
        var conn = await GetRabbitMQConnection(connection, factory, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        IChannel channel = null;

        try
        {
            channel = await conn.CreateChannelAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("The connection cannot support any more channels."))
            {
                conn = await GetRabbitMQConnection(connection, factory, cancellationToken, true);

                if (conn == null) throw new Exception("FAIL! Failed to create new connection for channel.", ex);
                channel = await conn.CreateChannelAsync(cancellationToken: cancellationToken);

                if (channel == null) throw new Exception("FAIL! Failed to create new channel.", ex);
            }
        }

        return channel ?? throw new Exception("Failed to create channel.");
    }

    private static readonly SemaphoreSlim ConnSemaphore = new(1, 1);

    private static async Task<IConnection> GetRabbitMQConnection(Connection connection, ConnectionFactory factory,
        CancellationToken cancellationToken, bool forceCreate = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cacheKey = GenerateCacheKey(connection);

        // Try to get a cached connection (fast path)
        if (!forceCreate &&
            RabbitMqConnectionCache.Get(cacheKey) is IConnection { IsOpen: true } cached1)
        {
            return cached1;
        }

        await ConnSemaphore.WaitAsync(cancellationToken);

        try
        {
            // Check cache AGAIN (someone may have beaten us)
            if (!forceCreate &&
                RabbitMqConnectionCache.Get(cacheKey) is IConnection { IsOpen: true } cached2)
            {
                return cached2;
            }

            var created = await factory.CreateConnectionAsync(cancellationToken);


            if (forceCreate)
            {
                RabbitMqConnectionCache.Remove(cacheKey);
            }


            // Try to insert our connection
            var added = RabbitMqConnectionCache.Add(cacheKey, created,
                new CacheItemPolicy
                {
                    RemovedCallback = RemovedCallback,
                    SlidingExpiration = TimeSpan.FromSeconds(connection.ConnectionExpirationSeconds)
                });

            if (added)
            {
                return created;
            }

            // Something else was added between Try & Add
            if (RabbitMqConnectionCache.Get(cacheKey) is IConnection { IsOpen: true } cached3)
            {
                await created.CloseAsync(cancellationToken);
                created.Dispose();

                return cached3;
            }

            await created.CloseAsync(cancellationToken);
            created.Dispose();

            throw new Exception("Failed to create connection.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Operation failed: {ex.Message}", ex);
        }
        finally
        {
            ConnSemaphore.Release();
        }
    }

    [ExcludeFromCodeCoverage]
    private static string GenerateCacheKey(Connection connection)
    {
        var virtualHost = string.IsNullOrWhiteSpace(connection.VirtualHost) ? "/" : connection.VirtualHost;
        var key = $"{connection.Host}:{connection.Timeout}:{virtualHost}";

        if (connection.AuthenticationMethod == AuthenticationMethod.Host)
        {
            key += $":{connection.Username}:{connection.Password}:{connection.Port}";
        }
        else if (connection.AuthenticationMethod == AuthenticationMethod.Certificate)
        {
            key += $":cert:{connection.Port}:{connection.CertificateSource}";
            key += connection.CertificateSource switch
            {
                CertificateSource.File => $":{connection.ClientCertificatePath}",
                CertificateSource.Store => $":{connection.StoreThumbprint}:{connection.CertificateStoreLocation}",
                CertificateSource.Base64 => $":{connection.CertificateBase64}",
                CertificateSource.RawBytes => $":{Convert.ToBase64String(connection.CertificateBytes)}",
                _ => string.Empty
            };
        }
        else if (connection.AuthenticationMethod == AuthenticationMethod.CertificateWithCredentials)
        {
            key += $":certcreds:{connection.Port}:{connection.Username}:{connection.Password}:{connection.CertificateSource}";
            key += connection.CertificateSource switch
            {
                CertificateSource.File => $":{connection.ClientCertificatePath}",
                CertificateSource.Store => $":{connection.StoreThumbprint}:{connection.CertificateStoreLocation}",
                CertificateSource.Base64 => $":{connection.CertificateBase64}",
                CertificateSource.RawBytes => $":{Convert.ToBase64String(connection.CertificateBytes)}",
                _ => string.Empty
            };
        }

        return key;
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
