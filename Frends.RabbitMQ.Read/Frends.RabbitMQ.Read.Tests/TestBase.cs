using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.RabbitMQ.Read.Tests;

public abstract class TestBase
{
    private static readonly object Lock = new();
    protected const string RabbitHostName = "localhost";
    protected const string RabbitUsername = "agent";
    protected const string RabbitPassword = "agent123";
    private const string CertificateMappedUsername = "CN=agent";

    private static IContainer? rabbitContainer;
    private static IContainer? rabbitAllowInvalidContainer;
    private static int refCount;
    private static bool isInitialized;

    private static readonly string CertsDirPath = Path.Join(Directory.GetCurrentDirectory(), "TestData", "certs");
    private static readonly string ConfigsDirPath = Path.Join(Directory.GetCurrentDirectory(), "TestData", "configs");

    protected static void Initialize(TestContext testContext)
    {
        var shouldInitialize = false;
        lock (Lock)
        {
            refCount++;
            if (!isInitialized)
            {
                isInitialized = true;
                shouldInitialize = true;
            }
        }

        if (!shouldInitialize) return;

        rabbitContainer = new ContainerBuilder()
            .WithImage("rabbitmq:4.2.3-management")
            .WithHostname(RabbitHostName)

            .WithResourceMapping(Path.Combine(CertsDirPath, "ca_certificate.crt"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(CertsDirPath, "server_certificate.pem"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(CertsDirPath, "server_key.pem"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(ConfigsDirPath, "rabbitmq.conf"), "/etc/rabbitmq")
            .WithResourceMapping(Path.Combine(ConfigsDirPath, "enabled_plugins"), "/etc/rabbitmq")

            .WithEnvironment("RABBITMQ_DEFAULT_USER", RabbitUsername)
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", RabbitPassword)

            .WithPortBinding(5672, true)
            .WithPortBinding(5671, true)

            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(5672)
                .UntilInternalTcpPortIsAvailable(5671)
                .UntilMessageIsLogged(".*Server startup complete.*"))
            .Build();

        rabbitAllowInvalidContainer = new ContainerBuilder()
            .WithImage("rabbitmq:4.2.3-management")
            .WithHostname(RabbitHostName)
            .WithResourceMapping(Path.Combine(CertsDirPath, "ca_certificate.crt"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(CertsDirPath, "server_certificate.pem"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(CertsDirPath, "server_key.pem"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(ConfigsDirPath, "rabbitmq_allow_invalid.conf"), "/etc/rabbitmq")
            .WithResourceMapping(Path.Combine(ConfigsDirPath, "enabled_plugins"), "/etc/rabbitmq")
            .WithEnvironment("RABBITMQ_DEFAULT_USER", RabbitUsername)
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", RabbitPassword)
            .WithEnvironment("RABBITMQ_CONFIG_FILE", "/etc/rabbitmq/rabbitmq_allow_invalid")
            .WithPortBinding(5671, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(5671)
                .UntilMessageIsLogged(".*Server startup complete.*"))
            .Build();

        InitializeAsync().GetAwaiter().GetResult();
    }

    protected static int GetRabbitPort() => rabbitContainer?.GetMappedPublicPort(5672)
        ?? throw new InvalidOperationException("RabbitMQ test container has not been initialized.");

    protected static int GetRabbitSslPort() => rabbitContainer?.GetMappedPublicPort(5671)
        ?? throw new InvalidOperationException("RabbitMQ TLS test container has not been initialized.");

    protected static int GetRabbitAllowInvalidSslPort() => rabbitAllowInvalidContainer?.GetMappedPublicPort(5671)
        ?? throw new InvalidOperationException("RabbitMQ allow-invalid TLS test container has not been initialized.");

    protected static string GetRabbitUri() => $"amqp://{RabbitUsername}:{RabbitPassword}@{RabbitHostName}:{GetRabbitPort()}";

    private static async Task InitializeAsync()
    {
        await rabbitContainer?.StartAsync()!;
        await ConfigureContainerAsync(rabbitContainer);

        await rabbitAllowInvalidContainer?.StartAsync()!;
        await ConfigureContainerAsync(rabbitAllowInvalidContainer);
    }

    protected static void BaseCleanup()
    {
        bool shouldCleanup = false;

        lock (Lock)
        {
            refCount--;
            if (refCount <= 0)
            {
                shouldCleanup = true;
                isInitialized = false;
            }
        }

        if (!shouldCleanup) return;

        CleanupAsync().GetAwaiter().GetResult();
    }

    private static async Task CleanupAsync()
    {
        if (rabbitContainer != null)
        {
            await rabbitContainer.StopAsync();
            await rabbitContainer.DisposeAsync();
            rabbitContainer = null;
        }

        if (rabbitAllowInvalidContainer != null)
        {
            await rabbitAllowInvalidContainer.StopAsync();
            await rabbitAllowInvalidContainer.DisposeAsync();
            rabbitAllowInvalidContainer = null;
        }
    }

    private static async Task ConfigureContainerAsync(IContainer container)
    {
        await container.ExecAsync(new[]
        {
            "rabbitmqctl",
            "set_user_limits",
            RabbitUsername,
            "{\"max-connections\": 20}",
        });

        var users = await container.ExecAsync(new[] { "rabbitmqctl", "list_users" });
        if (!users.Stdout.Contains(CertificateMappedUsername, StringComparison.Ordinal))
        {
            await container.ExecAsync(new[]
            {
                "rabbitmqctl",
                "add_user",
                CertificateMappedUsername,
                RabbitPassword,
            });
        }

        await container.ExecAsync(new[]
        {
            "rabbitmqctl",
            "set_permissions",
            "-p",
            "/",
            CertificateMappedUsername,
            ".*",
            ".*",
            ".*",
        });
    }
}
