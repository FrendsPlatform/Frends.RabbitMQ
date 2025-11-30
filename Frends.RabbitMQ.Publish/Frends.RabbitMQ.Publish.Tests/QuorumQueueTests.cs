using Frends.RabbitMQ.Publish.Definitions;
using Frends.RabbitMQ.Publish.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Tasks;

namespace Frends.RabbitMQ.Publish.Tests;

[TestClass]
public class QuorumQueueTests : TestBase
{
    /// <summary>
    /// You will need access to RabbitMQ queue, you can create it e.g. by running
    /// docker run -d --hostname my-rabbit -p 5672:5672 -p 8080:1567 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=agent -e RABBITMQ_DEFAULT_PASS=agent123 rabbitmq:3.9-management
    /// In that case URI would be amqp://agent:agent123@localhost:5672
    /// Access UI from http://localhost:15672 username: agent, password: agent123
    /// </summary>
    private const string _testUri = "amqp://agent:agent123@localhost:5672";

    private const string _testHost = "localhost";
    private const string _queue = "quorum";
    private const string _exchange = "exchange";
    private static Header[] _headers = Array.Empty<Header>();

    [ClassInitialize]
    public static void Init(TestContext testContext) => Initialize(testContext);

    [ClassCleanup]
    public static void Cleanup() => BaseCleanup();

    [TestInitialize]
    public async Task CreateExchangeAndQueue()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_testUri) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(_exchange, type: "fanout", durable: false, autoDelete: false);
        var args = new Dictionary<string, object?> { ["x-queue-type"] = "quorum" };
        await channel.QueueDeclareAsync(_queue, durable: true, exclusive: false, autoDelete: false, arguments: args);
        await channel.QueueBindAsync(_queue, _exchange, routingKey: "");

        _headers = new Header[]
        {
            new() { Name = "X-AppId", Value = "application id" },
            new() { Name = "X-ClusterId", Value = "cluster id" },
            new() { Name = "Content-Type", Value = "content type" },
            new() { Name = "Content-Encoding", Value = "content encoding" },
            new() { Name = "X-CorrelationId", Value = "correlation id" },
            new() { Name = "X-Expiration", Value = "100" }, new() { Name = "X-MessageId", Value = "message id" },
            new() { Name = "Custom-Header", Value = "custom header" }
        };
    }

    [TestCleanup]
    public async Task DeleteExchangeAndQueue()
    {
        await Helper.DeleteQuorumQueue(_testUri, _queue, _exchange);
    }

    [TestMethod]
    public async Task TestPublishAsString()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = "quorum",
            QueueName = "quorum",
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            Quorum = true
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = _headers };

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);

        Assert.IsNotNull(readValues.Message);
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("String", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
        Assert.AreEqual(8, result.Headers.Count);
    }

    [TestMethod]
    public async Task TestPublishAsByteArray()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = "quorum",
            QueueName = "quorum",
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            Quorum = true
        };

        Input input = new()
        {
            DataByteArray = Encoding.UTF8.GetBytes("test message"),
            InputType = InputType.ByteArray,
            Headers = _headers
        };

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);

        Assert.IsNotNull(readValues.Message);
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("ByteArray", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
    }

    [TestMethod]
    public async Task TestPublishAsString_WithoutHeaders()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = "quorum",
            QueueName = "quorum",
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            Quorum = true
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = null };

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);
        Assert.IsNotNull(readValues.Message);
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("String", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
        Assert.AreEqual(0, result.Headers.Count);
    }

    [TestMethod]
    public async Task TestPublishAsByteArray_WithoutHeaders()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = "quorum",
            QueueName = "quorum",
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            Quorum = true
        };

        var input = new Input();
        input.DataByteArray = Encoding.UTF8.GetBytes("test message");
        input.InputType = InputType.ByteArray;
        input.Headers = null;

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);
        Assert.IsNotNull(readValues.Message);
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("ByteArray", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
    }

    [TestMethod]
    public async Task TestURIConnection()
    {
        Connection connection = new()
        {
            Host = _testUri,
            RoutingKey = "quorum",
            QueueName = "quorum",
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.URI,
            Quorum = true
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = _headers };

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);

        Assert.IsNotNull(readValues.Message);
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("String", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
    }

    [TestMethod]
    public async Task TestURIConnectionWithCreateQueue()
    {
        await Helper.DeleteQuorumQueue(_testUri, _queue);
        var newQueue = "newQuorum";
        Connection connection = new()
        {
            Host = _testUri,
            ExchangeName = _exchange,
            RoutingKey = "",
            QueueName = newQueue,
            Create = true,
            Durable = true,
            AutoDelete = false,
            Quorum = true,
            AuthenticationMethod = AuthenticationMethod.URI,
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = _headers };

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);

        Assert.IsNotNull(readValues.Message);
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("String", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));

        await Helper.DeleteQuorumQueue(_testUri, newQueue, _exchange);
    }
}
