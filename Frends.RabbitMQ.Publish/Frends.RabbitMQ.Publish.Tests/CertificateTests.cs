using System.Security.Cryptography.X509Certificates;
using Frends.RabbitMQ.Publish.Definitions;
using Frends.RabbitMQ.Publish.Tests.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Frends.RabbitMQ.Publish.Tests;

//To run those tests, ca_certificate.crt must be installed as trusted root certificate.
[TestClass]
public class CertificateTests
{
    private const string TestHost = "localhost";
    private const string Queue = "quorum";
    private const string Exchange = "exchange";
    private static Header[] headers = Array.Empty<Header>();
    private static readonly string CertsDirPath = Path.Join(Directory.GetCurrentDirectory(), "TestData", "certs");
    private static readonly string ConfigsDirPath = Path.Join(Directory.GetCurrentDirectory(), "TestData", "configs");

    private static IContainer? rabbitContainer;

    [ClassInitialize]
    public static void Init(TestContext testContext)
    {
        rabbitContainer = new ContainerBuilder()
            .WithImage("rabbitmq:4.2.3-management")
            .WithName("test-rabbitmq-ssl")
            .WithHostname("localhost")
            .WithResourceMapping(Path.Combine(CertsDirPath, "ca_certificate.crt"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(CertsDirPath, "server_certificate.pem"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(CertsDirPath, "server_key.pem"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(ConfigsDirPath, "rabbitmq.conf"), "/etc/rabbitmq")
            .WithResourceMapping(Path.Combine(ConfigsDirPath, "enabled_plugins"), "/etc/rabbitmq")
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "agent")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "agent123")
            .WithPortBinding(5671, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(".*Server startup complete.*"))
            .Build();

        InitializeAsync().GetAwaiter().GetResult();
    }

    private static async Task InitializeAsync()
    {
        await rabbitContainer?.StartAsync()!;
        await rabbitContainer.ExecAsync(new[]
        {
            "rabbitmqctl", "set_user_limits", "agent", "{\"max-connections\": 20}",
        });
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        CleanupAsync().GetAwaiter().GetResult();
    }

    private static async Task CleanupAsync()
    {
        if (rabbitContainer != null)
        {
            await rabbitContainer.StopAsync();
            await rabbitContainer.DisposeAsync();
        }
    }

    [TestInitialize]
    public async Task CreateExchangeAndQueue()
    {
        var factory = new ConnectionFactory();
        factory.HostName = "localhost";
        factory.Port = rabbitContainer.GetMappedPublicPort(5671);
        factory.Ssl.Enabled = true;
        factory.Ssl.ServerName = "localhost";
        factory.Ssl.Version = System.Security.Authentication.SslProtocols.None;
        var cert = new X509Certificate2(Path.Join(CertsDirPath, "client_certificate.pfx"), "pass");
        factory.Ssl.Certs = new X509Certificate2Collection(cert);
        factory.Ssl.CertificateValidationCallback = (_, _, _, _) => true;
        factory.AuthMechanisms = new List<IAuthMechanismFactory> { new ExternalMechanismFactory() };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(Exchange, type: "fanout", durable: false, autoDelete: false);
        await channel.QueueDeclareAsync(Queue, durable: false, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(Queue, Exchange, routingKey: "");
        headers = new Header[]
        {
            new()
            {
                Name = "X-AppId",
                Value = "application id"
            },
            new()
            {
                Name = "X-ClusterId",
                Value = "cluster id"
            },
            new()
            {
                Name = "Content-Type",
                Value = "content type"
            },
            new()
            {
                Name = "Content-Encoding",
                Value = "content encoding"
            },
            new()
            {
                Name = "X-CorrelationId",
                Value = "correlation id"
            },
            new()
            {
                Name = "X-Expiration",
                Value = "100"
            },
            new()
            {
                Name = "X-MessageId",
                Value = "message id"
            },
            new()
            {
                Name = "Custom-Header",
                Value = "custom header"
            }
        };
    }

    [TestCleanup]
    public async Task DeleteExchangeAndQueue()
    {
        var factory = new ConnectionFactory();
        factory.HostName = "localhost";
        factory.Port = rabbitContainer.GetMappedPublicPort(5671);
        factory.Ssl.Enabled = true;
        factory.Ssl.ServerName = "localhost";
        factory.Ssl.Version = System.Security.Authentication.SslProtocols.None;
        var cert = new X509Certificate2(Path.Join(CertsDirPath, "client_certificate.pfx"), "pass");
        factory.Ssl.Certs = new X509Certificate2Collection(cert);
        factory.Ssl.CertificateValidationCallback = (_, _, _, _) => true;
        factory.AuthMechanisms = new List<IAuthMechanismFactory> { new ExternalMechanismFactory() };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(Queue, false, false);
        await channel.ExchangeDeleteAsync(Exchange, ifUnused: false);
    }

    [TestMethod]
    public async Task TestCertFromFile()
    {
        Connection connection = new()
        {
            Timeout = 30,
            AuthenticationMethod = AuthenticationMethod.Certificate,
            Host = TestHost,
            Port = rabbitContainer.GetMappedPublicPort(5671),
            SslProtocol = SslProtocol.None,
            CertificateSource = CertificateSource.File,
            ClientCertificatePath = Path.Join(CertsDirPath, "client_certificate.pfx"),
            ClientCertificatePassword = "pass",
            QueueName = Queue,
            ExchangeName = "",
            RoutingKey = Queue,
            Create = false,
            AutoDelete = false,
            Durable = false,
        };

        Input input = new()
        {
            DataByteArray = Encoding.UTF8.GetBytes("test message"),
            InputType = InputType.ByteArray,
            Headers = headers,
        };

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(readValues.Message));
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("ByteArray", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
    }

    [TestMethod]
    public async Task TestCertFromBase64()
    {
        byte[] pfxBytes = File.ReadAllBytes(Path.Join(CertsDirPath, "client_certificate.pfx"));
        string base64Pfx = Convert.ToBase64String(pfxBytes);
        Connection connection = new()
        {
            Timeout = 30,
            AuthenticationMethod = AuthenticationMethod.Certificate,
            Host = TestHost,
            Port = rabbitContainer.GetMappedPublicPort(5671),
            SslProtocol = SslProtocol.None,
            CertificateSource = CertificateSource.Base64,
            CertificateBase64 = base64Pfx,
            ClientCertificatePassword = "pass",
            QueueName = Queue,
            ExchangeName = "",
            RoutingKey = Queue,
            Create = false,
            AutoDelete = false,
            Durable = false,
        };

        Input input = new()
        {
            DataByteArray = Encoding.UTF8.GetBytes("test message"),
            InputType = InputType.ByteArray,
            Headers = headers,
        };

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(readValues.Message));
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("ByteArray", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
    }

    [TestMethod]
    public async Task TestCertFromRawBytes()
    {
        byte[] pfxBytes = File.ReadAllBytes(Path.Join(CertsDirPath, "client_certificate.pfx"));
        Connection connection = new()
        {
            Timeout = 30,
            AuthenticationMethod = AuthenticationMethod.Certificate,
            Host = TestHost,
            Port = rabbitContainer.GetMappedPublicPort(5671),
            SslProtocol = SslProtocol.None,
            CertificateSource = CertificateSource.RawBytes,
            CertificateBytes = pfxBytes,
            ClientCertificatePassword = "pass",
            QueueName = Queue,
            ExchangeName = "",
            RoutingKey = Queue,
            Create = false,
            AutoDelete = false,
            Durable = false,
        };

        Input input = new()
        {
            DataByteArray = Encoding.UTF8.GetBytes("test message"),
            InputType = InputType.ByteArray,
            Headers = headers,
        };

        var readValues = new Helper.ReadValues();
        var result = await RabbitMQ.Publish(input, connection, default);
        await Helper.ReadMessage(readValues, connection);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(readValues.Message));
        Assert.AreEqual("test message", readValues.Message);
        Assert.AreEqual("ByteArray", result.DataFormat);
        Assert.AreEqual("test message", result.DataString);
        Assert.IsTrue(result.DataByteArray.SequenceEqual(Encoding.UTF8.GetBytes("test message")));
    }
}
