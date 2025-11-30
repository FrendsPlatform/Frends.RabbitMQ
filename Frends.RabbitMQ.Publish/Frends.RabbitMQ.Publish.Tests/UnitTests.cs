using System.Collections.Concurrent;
using Frends.RabbitMQ.Publish.Definitions;
using Frends.RabbitMQ.Publish.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using System.Text;

namespace Frends.RabbitMQ.Publish.Tests;

[TestClass]
public class UnitTests : TestBase
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
        await channel.QueueDeclareAsync(_queue, durable: false, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(_queue, _exchange, routingKey: "");

        await channel.ExchangeDeclareAsync("ex", type: "fanout", durable: false, autoDelete: false);


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
        var factory = new ConnectionFactory { Uri = new Uri(_testUri) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(_queue, false, false);
        await channel.ExchangeDeleteAsync(_exchange, ifUnused: false);
    }

    [TestMethod]
    public async Task TestPublishAsString()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            Timeout = 30
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = _headers };

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);

        Assert.IsTrue(!string.IsNullOrEmpty(readValues.Message));
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("String", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
        Assert.IsTrue(readValues.Headers.ContainsKey("X-AppId"));
        Assert.IsTrue(readValues.Headers.ContainsValue("application id"));
        Assert.IsTrue(result.Headers.ContainsKey("X-AppId"));
        Assert.IsTrue(result.Headers.ContainsValue("application id"));
        Assert.IsTrue(readValues.Headers.ContainsKey("X-ClusterId"));
        Assert.IsTrue(readValues.Headers.ContainsValue("cluster id"));
        Assert.IsTrue(result.Headers.ContainsKey("X-ClusterId"));
        Assert.IsTrue(result.Headers.ContainsValue("cluster id"));
        Assert.IsTrue(readValues.Headers.ContainsKey("Content-Type"));
        Assert.IsTrue(readValues.Headers.ContainsValue("content type"));
        Assert.IsTrue(result.Headers.ContainsKey("Content-Type"));
        Assert.IsTrue(result.Headers.ContainsValue("content type"));
        Assert.IsTrue(readValues.Headers.ContainsKey("Content-Encoding"));
        Assert.IsTrue(readValues.Headers.ContainsValue("content encoding"));
        Assert.IsTrue(result.Headers.ContainsKey("Content-Encoding"));
        Assert.IsTrue(result.Headers.ContainsValue("content encoding"));
        Assert.IsTrue(readValues.Headers.ContainsKey("X-CorrelationId"));
        Assert.IsTrue(readValues.Headers.ContainsValue("correlation id"));
        Assert.IsTrue(result.Headers.ContainsKey("X-CorrelationId"));
        Assert.IsTrue(result.Headers.ContainsValue("correlation id"));
        Assert.IsTrue(readValues.Headers.ContainsKey("X-Expiration"));
        Assert.IsTrue(readValues.Headers.ContainsValue("100"));
        Assert.IsTrue(result.Headers.ContainsKey("X-Expiration"));
        Assert.IsTrue(result.Headers.ContainsValue("100"));
        Assert.IsTrue(readValues.Headers.ContainsKey("X-MessageId"));
        Assert.IsTrue(readValues.Headers.ContainsValue("message id"));
        Assert.IsTrue(result.Headers.ContainsKey("X-MessageId"));
        Assert.IsTrue(result.Headers.ContainsValue("message id"));
        Assert.IsTrue(readValues.Headers.ContainsKey("Custom-Header"));
        Assert.IsTrue(readValues.Headers.ContainsValue("custom header"));
        Assert.IsTrue(result.Headers.ContainsKey("Custom-Header"));
        Assert.IsTrue(result.Headers.ContainsValue("custom header"));
    }

    [TestMethod]
    public async Task TestPublishAsByteArray()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Port = 5672,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = ""
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
    public async Task TestPublishAsString_WithoutHeadersAsync()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = ""
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
    public async Task TestPublishAsByteArray_WithoutHeadersAsync()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = ""
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
    public async Task TestURIConnectionAsync()
    {
        Connection connection = new()
        {
            Host = _testUri,
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.URI,
            Timeout = 0
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
    public async Task TestParallelConnections()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = ""
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = null };

        for (var i = 0; i < 30; i++)
        {
            var success = 0;
            var errors = 0;
            var errorList = new ConcurrentBag<string>();

            var tasks = Enumerable.Range(0, 50)
                .Select(async _ =>
                {
                    try
                    {
                        await RabbitMQ.Publish(input, connection, CancellationToken.None);
                        Interlocked.Increment(ref success);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref errors);
                        errorList.Add(ex.ToString());
                    }
                });

            await Task.WhenAll(tasks);

            Assert.AreEqual(0, errors);
        }
    }

    [TestMethod]
    public async Task TestMultipleRecurringCallsWithDifferentConnectionsAsync()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            Timeout = 60,
            ConnectionExpirationSeconds = 0
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = null };

        for (var i = 0; i < 30; i++)
        {
            connection.Timeout++;
            var readValues = new Helper.ReadValues();
            var result = await RabbitMQ.Publish(input, connection, default);
            await Helper.ReadMessage(readValues, connection);
            Assert.AreEqual("test message", readValues.Message);
            Assert.AreEqual("String", result.DataFormat);
            Assert.AreEqual("test message", result.DataString);
            Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
            Assert.AreEqual(0, result.Headers.Count);
        }
    }

    [TestMethod]
    public async Task TestMultipleRecurringCallsAsync()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            Timeout = 0,
            ConnectionExpirationSeconds = 30
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = null };

        for (var i = 0; i < 30; i++)
        {
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
    }

    [TestMethod]
    public async Task TestMultipleRecurringCallsWithConnectionExpirationSetToZeroAsync()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            Timeout = 0,
            ConnectionExpirationSeconds = 0
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = null };

        for (var i = 0; i < 30; i++)
        {
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
    }

    [TestMethod]
    public async Task TestInvalidCredentials()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "foo",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            ConnectionExpirationSeconds = 0
        };

        Input input = new() { DataString = "test message", InputType = InputType.String, Headers = null };

        var ex = await Assert.ThrowsAsync<Exception>(() => RabbitMQ.Publish(input, connection, default));
        Assert.AreEqual("Operation failed: None of the specified endpoints were reachable",
            ex.Message);
    }

    [TestMethod]
    public async Task TestWithoutMessage()
    {
        Connection connection = new()
        {
            Host = _testHost,
            Username = "agent",
            Password = "agent123",
            RoutingKey = _queue,
            QueueName = _queue,
            Create = false,
            Durable = false,
            AutoDelete = false,
            AuthenticationMethod = AuthenticationMethod.Host,
            ExchangeName = "",
            ConnectionExpirationSeconds = 0
        };

        Input input = new() { DataString = "", InputType = InputType.String, Headers = null };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => RabbitMQ.Publish(input, connection, default));
        Assert.AreEqual("Publish: Message data is missing.", ex.Message);
    }
}
