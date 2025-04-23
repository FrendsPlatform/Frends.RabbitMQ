using System.Threading.Tasks;
using Frends.RabbitMQ.Read.Definitions;
using RabbitMQ.Client;

namespace Frends.RabbitMQ.Read.Tests.Lib;
internal class Helper
{
    internal static async Task OpenConnectionIfClosed(ConnectionHelper connectionHelper, Connection connection)
    {
        if (IsConnectionHostNameChanged(connectionHelper, connection))
            await connectionHelper.AMQPModel.CloseAsync();

        if (connectionHelper.AMQPConnection == null || connectionHelper.AMQPConnection.IsOpen == false)
        {
            var factory = new ConnectionFactory();

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
            }

            if (connection.Timeout != 0) factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(connection.Timeout);

            connectionHelper.AMQPConnection = await factory.CreateConnectionAsync();
        }

        if (connectionHelper.AMQPModel == null || connectionHelper.AMQPModel.IsClosed)
            connectionHelper.AMQPModel = await connectionHelper.AMQPConnection.CreateChannelAsync();
    }

    internal static bool IsConnectionHostNameChanged(ConnectionHelper connectionHelper, Connection connection)
    {
        if (connectionHelper.AMQPConnection == null || connectionHelper.AMQPConnection.IsOpen == false)
            return false;

        switch (connection.AuthenticationMethod)
        {
            case AuthenticationMethod.URI:
                var newUri = new Uri(connection.Host);
                return (connectionHelper.AMQPConnection.Endpoint.HostName != newUri.Host);
            case AuthenticationMethod.Host:
                return (connectionHelper.AMQPConnection.Endpoint.HostName != connection.Host);
            default:
                throw new ArgumentException($"IsConnectionHostNameChanged: AuthenticationMethod missing.");
        }
    }
}

