using Frends.RabbitMQ.Read.Definitions;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Frends.RabbitMQ.Read
{
    internal class Helper
    {
        internal static Dictionary<string, string> GetResponseHeaderDictionary(IReadOnlyBasicProperties basicProperties)
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
                            ConfigureCommonSsl(factory, connection);
                            certToDispose = LoadCertificate(connection);
                            factory.Ssl.Certs = new X509Certificate2Collection(certToDispose);
                            if (connection.AllowInvalidCertificate)
                                factory.Ssl.CertificateValidationCallback = (_, _, _, _) => true;
                            factory.AuthMechanisms = new IAuthMechanismFactory[] { new ExternalMechanismFactory() };
                            break;

                        case AuthenticationMethod.CertificateWithCredentials:
                            ConfigureCommonSsl(factory, connection);
                            factory.UserName = connection.Username;
                            factory.Password = connection.Password;
                            certToDispose = LoadCertificate(connection);
                            factory.Ssl.Certs = new X509Certificate2Collection(certToDispose);
                            if (connection.AllowInvalidCertificate)
                                factory.Ssl.CertificateValidationCallback = (_, _, _, _) => true;
                            break;
                    }

                    if (connection.AuthenticationMethod != AuthenticationMethod.URI && !string.IsNullOrWhiteSpace(connection.VirtualHost))
                        factory.VirtualHost = connection.VirtualHost;

                    if (connection.Timeout != 0) factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(connection.Timeout);

                    connectionHelper.AMQPConnection = await factory.CreateConnectionAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Operation failed: {ex.Message}", ex);
                }
                finally
                {
                    certToDispose?.Dispose();
                }
            }

            if (connectionHelper.AMQPModel == null || connectionHelper.AMQPModel.IsClosed)
            {
                try
                {
                    connectionHelper.AMQPModel = await connectionHelper.AMQPConnection.CreateChannelAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to create channel.", ex);
                }
            }
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

        private static void ConfigureCommonSsl(ConnectionFactory factory, Connection connection)
        {
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
        }

        private static X509Certificate2 LoadCertificate(Connection connection)
        {
            return connection.CertificateSource switch
            {
                CertificateSource.File => new X509Certificate2(connection.ClientCertificatePath, connection.ClientCertificatePassword),
                CertificateSource.Base64 => new X509Certificate2(Convert.FromBase64String(connection.CertificateBase64), connection.ClientCertificatePassword),
                CertificateSource.RawBytes => new X509Certificate2(connection.CertificateBytes, connection.ClientCertificatePassword),
                CertificateSource.Store => LoadFromStore(connection.StoreThumbprint, connection.CertificateStoreLocation),
                _ => throw new InvalidEnumArgumentException("Unknown certificate source.")
            };
        }
    }
}
