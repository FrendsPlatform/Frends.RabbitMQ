using Frends.RabbitMQ.Read.Definitions;
using Frends.RabbitMQ.Read.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Tasks;

namespace Frends.RabbitMQ.Read.Tests;

[TestClass]
public class QuorumQueueTests
{
    /// <summary>
    /// You will need access to RabbitMQ queue, you can create it e.g. by running
    /// docker run -d --hostname my-rabbit -p 5672:5672 -p 8080:1567 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=agent -e RABBITMQ_DEFAULT_PASS=agent123  rabbitmq:3.9-management
    /// In that case URI would be amqp://agent:agent123@localhost:5672 
    /// Access UI from http://localhost:15672 username: agent, password: agent123
    /// </summary>

    private const string _testUri = "amqp://agent:agent123@localhost:5672";
    private const string _testHost = "localhost";
    private const string _exchange = "exchange";
    private const string _queue = "quorumqueue";
    private const string _username = "agent";
    private const string _pws = "agent123";

    [TestInitialize]
    public async Task CreateExchangeAndQueue()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_testUri) };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(_exchange, type: "fanout", durable: false, autoDelete: false);
        var args = new Dictionary<string, object?>();
        args["x-queue-type"] = "quorum";
        await channel.QueueDeclareAsync(_queue, durable: true, exclusive: false, autoDelete: false, arguments: args);
        await channel.QueueBindAsync(_queue, _exchange, routingKey: "");
    }

    [TestCleanup]
    public async Task DeleteExchangeAndQueue()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_testUri) };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(_queue, false, false);
        await channel.ExchangeDeleteAsync(_exchange, ifUnused: false);
    }

    [TestMethod]
    public async Task TestReadMultipleMessagesWithHostQuorum()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = _username,
            Password = _pws,
            RoutingKey = _queue,
            QueueName = _queue,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = _exchange,

            AutoAck = ReadAckType.AutoAck,
            ReadMessageCount = 2,
        };

        await Publish(connection, 2);
        var result = await RabbitMQ.Read(connection);

        Assert.AreEqual(2, result.MessagesBase64.Count);
        Assert.AreEqual(2, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDE=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 1")));

        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsKey("X-AppId")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsValue("application id")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsKey("X-AppId")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsValue("cluster id")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsKey("Content-Type")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsValue("content type")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsKey("Content-Encoding")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsValue("content encoding")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsKey("X-CorrelationId")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsValue("correlation id")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsKey("X-Expiration")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsValue("100")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsKey("X-MessageId")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsValue("message id")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsKey("Custom-Header")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Headers.ContainsValue("custom header")));

        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsKey("X-AppId")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsValue("application id")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsKey("X-AppId")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsValue("cluster id")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsKey("Content-Type")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsValue("content type")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsKey("Content-Encoding")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsValue("content encoding")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsKey("X-CorrelationId")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsValue("correlation id")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsKey("X-Expiration")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsValue("100")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsKey("X-MessageId")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsValue("message id")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsKey("Custom-Header")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Headers.ContainsValue("custom header")));
    }

    /// <summary>
    /// Connect with URI and read single message.
    /// </summary>
    [TestMethod]
    public async Task TestReadSingleMessageWithURIQuorum()
    {
        Connection connection = new()
        {
            Host = _testUri,
            RoutingKey = _queue,
            QueueName = _queue,
            AuthenticationMethod = AuthenticationMethod.URI,
            ExchangeName = null,

            AutoAck = ReadAckType.AutoAck,
            ReadMessageCount = 1,
        };

        await Publish(connection, 1);
        var result = await RabbitMQ.Read(connection);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
    }

    public static async Task Publish(Connection connection, int messageCount)
    {
        ConnectionHelper connectionHelper = new();
        var message = "Test message";

        await Helper.OpenConnectionIfClosed(connectionHelper, connection);

        var args = new Dictionary<string, object?>();
        args.Add("x-queue-type", "quorum");

        await connectionHelper.AMQPModel.QueueDeclareAsync(queue: connection.QueueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: args);

        BasicProperties basicProperties = new()
        {
            Persistent = false
        };

        var headers = new Dictionary<string, object?>() {
                { "X-AppId", "application id" },
                { "X-ClusterId", "cluster id" },
                { "Content-Type", "content type" },
                { "Content-Encoding", "content encoding" },
                { "X-CorrelationId", "correlation id" },
                { "X-Expiration", "100" },
                { "X-MessageId", "message id" },
                { "Custom-Header", "custom header" }
        };

        basicProperties.Headers = headers;

        for (var i = 0; i < messageCount; i++)
            await connectionHelper.AMQPModel.BasicPublishAsync(exchange: _exchange,
                routingKey: connection.RoutingKey,
                mandatory: true,
                basicProperties: basicProperties,
                body: Encoding.UTF8.GetBytes(message + " " + i));
    }
}

