using RabbitMQ.Client;

namespace Frends.RabbitMQ.Read.Tests.Lib;
internal class Helper
{
    internal static async Task DeleteQuorumQueue(string uri, string queue, string? exchange = null)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(uri)
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(queue, false, false);
        if (exchange != null)
            await channel.ExchangeDeleteAsync(exchange, ifUnused: false);
    }
}
