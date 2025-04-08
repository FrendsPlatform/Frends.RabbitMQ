using RabbitMQ.Client;
using System.Text;
using Frends.RabbitMQ.Publish.Definitions;
using System.Threading.Tasks;

namespace Frends.RabbitMQ.Publish.Tests.Lib;
internal class Helper
{
    internal static async Task ReadMessage(ReadValues readValues, Connection connection)
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
                break;
        }

        await using IConnection _connection = await factory.CreateConnectionAsync();
        await using IChannel _model = await _connection.CreateChannelAsync();

        var rcvMessage = await _model.BasicGetAsync(connection.QueueName, true);
        if (rcvMessage != null)
        {
            var message = Encoding.UTF8.GetString(rcvMessage.Body.ToArray());
            readValues.Message = message;
            readValues.Tag = rcvMessage.DeliveryTag;

            var data = new Dictionary<string, string>();
            if (rcvMessage.BasicProperties.IsHeadersPresent())
            {
                foreach (var header in rcvMessage.BasicProperties.Headers!.ToList())
                {
                    if (header.Value?.GetType() == typeof(byte[]))
                        data[header.Key] = Encoding.UTF8.GetString((byte[])header.Value);
                    else
                    {
                        string? value = header.Value?.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                            data[header.Key] = value;
                        else
                            data[header.Key] = "";
                    }
                }
                readValues.Headers = data;
            }
        }
    }

    internal static async Task DeleteQuorumQueue(string uri, string queue, string? exchange = null)
    {
        var factory = new ConnectionFactory { Uri = new Uri(uri) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(queue, false, false);
        if (exchange != null)
            await channel.ExchangeDeleteAsync(exchange, ifUnused: false);
    }

    internal class ReadValues
    {
        public string Message { get; set; } = "";
        public ulong Tag { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}

