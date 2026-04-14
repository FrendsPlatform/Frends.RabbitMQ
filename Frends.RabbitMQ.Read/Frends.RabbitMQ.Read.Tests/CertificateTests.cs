using System.Security.Cryptography.X509Certificates;
using Frends.RabbitMQ.Read.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Frends.RabbitMQ.Read.Tests;

//To run those tests, ca_certificate.crt must be installed as trusted root certificate.
[TestClass]
public class CertificateTests : TestBase
{
    private const string TestHost = "localhost";
    private const string Queue = "quorum";
    private const string Exchange = "exchange";

    private static readonly string CertsDirPath = Path.Join(
        Directory.GetCurrentDirectory(), "TestData", "certs");
    private static Options? options;

    private static Connection DefaultConnection() => new()
    {
        Timeout = 30,
        AuthenticationMethod = AuthenticationMethod.Certificate,
        Host = TestHost,
        Port = Container.GetMappedPublicPort(5671),
        SslProtocol = SslProtocol.None,
        QueueName = Queue,
        ExchangeName = "",
        RoutingKey = Queue,
        AckType = AckType.AutoAck,
        ReadMessageCount = 1,
    };

    [ClassInitialize]
    public static void Init(TestContext testContext) => Initialize(testContext);

    [ClassCleanup]
    public static void Cleanup() => BaseCleanup();

    [TestInitialize]
    public async Task CreateExchangeAndQueue()
    {
        var factory = new ConnectionFactory();
        factory.HostName = TestHost;
        factory.Port = Container.GetMappedPublicPort(5671);
        factory.Ssl.Enabled = true;
        factory.Ssl.ServerName = TestHost;
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
        await PublishTestMessage(channel);
        options = new Options();
    }

    [TestCleanup]
    public async Task DeleteExchangeAndQueue()
    {
        var factory = new ConnectionFactory();
        factory.HostName = TestHost;
        factory.Port = Container.GetMappedPublicPort(5671);
        factory.Ssl.Enabled = true;
        factory.Ssl.ServerName = TestHost;
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

    private static async Task PublishTestMessage(IChannel channel)
    {
        var body = Encoding.UTF8.GetBytes("test message");
        var props = new BasicProperties();
        await channel.BasicPublishAsync(exchange: Exchange, routingKey: Queue, mandatory: false,
            basicProperties: props, body: body);
    }

    [TestMethod]
    public async Task TestCertFromFile()
    {
        var conn = DefaultConnection();
        conn.CertificateSource = CertificateSource.File;
        conn.ClientCertificatePath = Path.Join(CertsDirPath, "client_certificate.pfx");
        conn.ClientCertificatePassword = "pass";

        var result = await RabbitMQ.Read(conn, options);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.AreEqual("test message", result.MessageUTF8[0].Data);
    }

    [TestMethod]
    public async Task TestCertFromBase64()
    {
        var pfxBytes = await File.ReadAllBytesAsync(Path.Join(CertsDirPath, "client_certificate.pfx"));
        var base64Pfx = Convert.ToBase64String(pfxBytes);
        var conn = DefaultConnection();
        conn.CertificateSource = CertificateSource.Base64;
        conn.CertificateBase64 = base64Pfx;
        conn.ClientCertificatePassword = "pass";

        var result = await RabbitMQ.Read(conn, options);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.AreEqual("test message", result.MessageUTF8[0].Data);
    }

    [TestMethod]
    public async Task TestCertFromRawBytes()
    {
        var pfxBytes = await File.ReadAllBytesAsync(Path.Join(CertsDirPath, "client_certificate.pfx"));
        var conn = DefaultConnection();
        conn.CertificateSource = CertificateSource.RawBytes;
        conn.CertificateBytes = pfxBytes;
        conn.ClientCertificatePassword = "pass";

        var result = await RabbitMQ.Read(conn, options);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.AreEqual("test message", result.MessageUTF8[0].Data);
    }

    [TestMethod]
    public async Task CertificateWithCredentials_BothCorrect_ShouldSucceed()
    {
        var conn = DefaultConnection();
        conn.AuthenticationMethod = AuthenticationMethod.CertificateWithCredentials;
        conn.Username = "agent";
        conn.Password = "agent123";
        conn.CertificateSource = CertificateSource.File;
        conn.ClientCertificatePath = Path.Join(CertsDirPath, "client_certificate.pfx");
        conn.ClientCertificatePassword = "pass";

        var result = await RabbitMQ.Read(conn, options);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.MessageUTF8.Count);
        Assert.AreEqual("test message", result.MessageUTF8[0].Data);
    }

    [TestMethod]
    public async Task CertificateWithCredentials_WrongPassword_ShouldFail()
    {
        var conn = DefaultConnection();
        conn.AuthenticationMethod = AuthenticationMethod.CertificateWithCredentials;
        conn.Username = "agent";
        conn.Password = "wrong-password";
        conn.CertificateSource = CertificateSource.File;
        conn.ClientCertificatePath = Path.Join(CertsDirPath, "client_certificate.pfx");
        conn.ClientCertificatePassword = "pass";

        var ex = await Assert.ThrowsExceptionAsync<Exception>(
            () => RabbitMQ.Read(conn, options));

        Assert.IsTrue(ex.Message.Contains("None of the specified endpoints were reachable"));
    }

    [TestMethod]
    public async Task CertificateWithCredentials_UntrustedCertificate_ShouldFail()
    {
        var conn = DefaultConnection();
        conn.AuthenticationMethod = AuthenticationMethod.CertificateWithCredentials;
        conn.Username = "agent";
        conn.Password = "agent123";
        conn.CertificateSource = CertificateSource.File;
        conn.ClientCertificatePath = Path.Join(CertsDirPath, "rogue_client_certificate.pfx");
        conn.ClientCertificatePassword = "pass";

        var ex = await Assert.ThrowsExceptionAsync<Exception>(
            () => RabbitMQ.Read(conn, options));

        Assert.IsTrue(ex.Message.Contains("None of the specified endpoints were reachable"));
    }
}
