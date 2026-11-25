using System.Text;
using Frends.RabbitMQ.Read.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;

namespace Frends.RabbitMQ.Read.Tests;

[TestClass]
public class UnitTests : TestBase
{
    private const string TestHost = RabbitHostName;
    private const string Exchange = "exchange";
    private const string Queue = "queue";
    private const string Username = RabbitUsername;
    private const string Psw = RabbitPassword;
    private static Options? options;

    /// <summary>
    /// URI is resolved from the RabbitMQ test container's mapped AMQP port.
    /// </summary>
    private static string TestUri => GetRabbitUri();

    [ClassInitialize]
    public static void Init(TestContext testContext) => Initialize(testContext);

    [ClassCleanup]
    public static void Cleanup() => BaseCleanup();

    public static async Task Publish(Connection connection, int messageCount, Dictionary<string, object?>? args = null)
    {
        ConnectionHelper connectionHelper = new();
        var message = "Test message";

        await Helper.OpenConnectionIfClosed(connectionHelper, connection);

        await connectionHelper.AMQPModel.QueueDeclareAsync(
            queue: Queue,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: args);

        BasicProperties basicProperties = new()
        {
            Persistent = false,
        };

        var headers = new Dictionary<string, object?>()
        {
                { "X-AppId", "application id" },
                { "X-ClusterId", "cluster id" },
                { "Content-Type", "content type" },
                { "Content-Encoding", "content encoding" },
                { "X-CorrelationId", "correlation id" },
                { "X-Expiration", "100" },
                { "X-MessageId", "message id" },
                { "Custom-Header", "custom header" },
        };

        basicProperties.Headers = headers;

        for (var i = 0; i < messageCount; i++)
        {
            await connectionHelper.AMQPModel.BasicPublishAsync(
                exchange: "exchange",
                routingKey: Queue,
                mandatory: true,
                basicProperties: basicProperties,
                body: Encoding.UTF8.GetBytes(message + " " + i));
        }
    }

    [TestInitialize]
    public async Task CreateExchangeAndQueue()
    {
        var factory = new ConnectionFactory { Uri = new Uri(GetRabbitUri()) };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(Exchange, type: "fanout", durable: false, autoDelete: false);
        await channel.QueueDeclareAsync(Queue, durable: false, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(Queue, Exchange, routingKey: string.Empty);
        options = new Options();
    }

    [TestCleanup]
    public async Task DeleteExchangeAndQueue()
    {
        var factory = new ConnectionFactory { Uri = new Uri(GetRabbitUri()) };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(Queue, false, false);
        await channel.ExchangeDeleteAsync(Exchange, ifUnused: false);
    }

    /// <summary>
    /// Connect with hostname and read single message.
    /// </summary>
    [TestMethod]
    public async Task TestReadSingleMessageWithHostAutoAckAsync()
    {
        Connection connection = new()
        {
            Host = TestHost,
            Port = GetRabbitPort(),
            Username = Username,
            Password = Psw,
            AuthenticationMethod = AuthenticationMethod.Host,
        };

        await Publish(connection, 1);
        var result = await Read(connection);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));

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
    /// Connect with hostname and read multiple messages.
    /// </summary>
    [TestMethod]
    public async Task TestReadMultipleMessagesWithHostAutoNack()
    {
        Connection connection = new()
        {
            Host = TestHost,
            Port = GetRabbitPort(),
            Username = Username,
            Password = Psw,
            AuthenticationMethod = AuthenticationMethod.Host,
        };

        await Publish(connection, 2);
        var result = await Read(connection, AckType.AutoNack, 2);

        var test1 = result.MessagesBase64.Count;
        var test2 = result.MessageUTF8.Count;

        Assert.IsTrue(result.MessagesBase64.Count > 1);
        Assert.IsTrue(result.MessageUTF8.Count > 1);
        Assert.IsTrue(result.Success);
    }

    /// <summary>
    /// Connect with URI and read single message.
    /// </summary>
    [TestMethod]
    public async Task TestReadSingleMessageWithUriAutoNack()
    {
        Connection connection = new()
        {
            Host = TestUri,
            AuthenticationMethod = AuthenticationMethod.URI,
        };

        await Publish(connection, 1);
        var result = await Read(connection, AckType.AutoNack);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
    }

    /// <summary>
    /// Connect with URI and read single message.
    /// </summary>
    [TestMethod]
    public async Task TestReadSingleMessageWithNoAck()
    {
        Connection connection = new()
        {
            Host = TestUri,
            AuthenticationMethod = AuthenticationMethod.URI,
        };

        await Publish(connection, 1);
        var result = await Read(connection, AckType.NoAck);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
    }

    [TestMethod]
    public async Task TestReadSingleMessageWithHostAutoNackAndRequeue()
    {
        Connection connection = new()
        {
            Host = TestHost,
            Port = GetRabbitPort(),
            Username = Username,
            Password = Psw,
            AuthenticationMethod = AuthenticationMethod.Host,
        };

        await Publish(connection, 1);
        var result = await Read(connection, AckType.AutoNackAndRequeue);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
    }

    /// <summary>
    /// Connect with hostname and read multiple messages.
    /// </summary>
    [TestMethod]
    public async Task TestReadMultipleMessagesWithHostAutoNackAndRequeue()
    {
        Connection connection = new()
        {
            Host = TestHost,
            Port = GetRabbitPort(),
            Username = Username,
            Password = Psw,
            AuthenticationMethod = AuthenticationMethod.Host,
        };

        await Publish(connection, 2);
        var result = await Read(connection, AckType.AutoNackAndRequeue, 2);

        var test1 = result.MessagesBase64.Count;
        var test2 = result.MessageUTF8.Count;

        Assert.AreEqual(2, result.MessagesBase64.Count);
        Assert.AreEqual(2, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDE=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 1")));
    }

    /// <summary>
    /// Connect with URI and read single message.
    /// </summary>
    [TestMethod]
    public async Task TestReadSingleMessageWithUriAutoNackAndRequeue()
    {
        Connection connection = new()
        {
            Host = TestUri,
            AuthenticationMethod = AuthenticationMethod.URI,
        };

        await Publish(connection, 1);
        var result = await Read(connection, AckType.AutoNackAndRequeue);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
    }

    [TestMethod]
    public async Task TestReadSingleMessageWithHostAutoReject()
    {
        Connection connection = new()
        {
            Host = TestHost,
            Port = GetRabbitPort(),
            Username = Username,
            Password = Psw,
            AuthenticationMethod = AuthenticationMethod.Host,
        };

        await Publish(connection, 1);
        var result = await Read(connection, AckType.AutoReject);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
    }

    /// <summary>
    /// Connect with hostname and read multiple messages.
    /// </summary>
    [TestMethod]
    public async Task TestReadMultipleMessagesWithHostAutoReject()
    {
        Connection connection = new()
        {
            Host = TestHost,
            Port = GetRabbitPort(),
            Username = Username,
            Password = Psw,
            AuthenticationMethod = AuthenticationMethod.Host,
        };

        await Publish(connection, 2);
        var result = await Read(connection, AckType.AutoReject, 2);

        var test1 = result.MessagesBase64.Count;
        var test2 = result.MessageUTF8.Count;

        Assert.AreEqual(2, result.MessagesBase64.Count);
        Assert.AreEqual(2, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDE=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 1")));
    }

    /// <summary>
    /// Connect with URI and read single message.
    /// </summary>
    [TestMethod]
    public async Task TestReadSingleMessageWithUriAutoReject()
    {
        Connection connection = new()
        {
            Host = TestUri,
            AuthenticationMethod = AuthenticationMethod.URI,
        };

        await Publish(connection, 1);
        var result = await Read(connection, AckType.AutoReject);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
    }

    [TestMethod]
    public async Task TestReadSingleMessageWithHostAutoRejectAndRequeue()
    {
        Connection connection = new()
        {
            Host = TestHost,
            Port = GetRabbitPort(),
            Username = Username,
            Password = Psw,
            AuthenticationMethod = AuthenticationMethod.Host,
        };

        await Publish(connection, 1);
        var result = await Read(connection, AckType.AutoRejectAndRequeue);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
    }

    /// <summary>
    /// Connect with hostname and read multiple messages.
    /// </summary>
    [TestMethod]
    public async Task TestReadMultipleMessagesWithHostAutoRejectAndRequeue()
    {
        Connection connection = new()
        {
            Host = TestHost,
            Port = GetRabbitPort(),
            Username = Username,
            Password = Psw,
            AuthenticationMethod = AuthenticationMethod.Host,
        };

        await Publish(connection, 2);
        var result = await Read(connection, AckType.AutoRejectAndRequeue, 2);

        Assert.AreEqual(2, result.MessagesBase64.Count);
        Assert.AreEqual(2, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
    }

    /// <summary>
    /// Connect with URI and read single message.
    /// </summary>
    [TestMethod]
    public async Task TestReadSingleMessageWithUriAutoRejectAndRequeue()
    {
        Connection connection = new()
        {
            Host = TestUri,
            AuthenticationMethod = AuthenticationMethod.URI,
        };

        await Publish(connection, 1);
        var result = await Read(connection, AckType.AutoRejectAndRequeue);

        Assert.AreEqual(1, result.MessagesBase64.Count);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.MessagesBase64.Any(x => x.Data.Equals("VGVzdCBtZXNzYWdlIDA=")));
        Assert.IsTrue(result.MessageUTF8.Any(x => x.Data.Equals("Test message 0")));
    }

    [TestMethod]
    public async Task TestPublishWithWrongVirtualHost_ShouldFail()
    {
        Connection connection = new()
        {
            Host = TestHost,
            Port = GetRabbitPort(),
            Username = "agent",
            Password = "agent123",
            AuthenticationMethod = AuthenticationMethod.Host,
            Timeout = 30,
            VirtualHost = "/nonexistent",
        };

        var ex = await Assert.ThrowsExceptionAsync<Exception>(
            () => Read(connection));

        Assert.IsTrue(ex.Message.Contains("Failed to create channel"));
    }

    [TestMethod]
    public async Task Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        var options = new Options
        {
            ThrowErrorOnFailure = false,
        };

        var result = await RabbitMQ.Read(new Input(), new Connection(), options);

        Assert.IsFalse(result.Success, "Expected result.Success to be false");
    }

    private static Task<Result> Read(Connection connection, AckType ackType = AckType.AutoAck, int readMessageCount = 1) =>
        RabbitMQ.Read(
            new Input
            {
                QueueName = Queue,
                AckType = ackType,
                ReadMessageCount = readMessageCount,
            },
            connection,
            options);
}
