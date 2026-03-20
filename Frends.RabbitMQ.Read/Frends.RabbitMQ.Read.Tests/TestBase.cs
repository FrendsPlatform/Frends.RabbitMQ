using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.RabbitMQ.Read.Tests;

public abstract class TestBase
{
    private static readonly object Lock = new();

    private static IContainer? rabbitContainer;
    private static int refCount;
    private static bool isInitialized;

    private static readonly string CertsDirPath = Path.Join(Directory.GetCurrentDirectory(), "TestData", "certs");
    private static readonly string ConfigsDirPath = Path.Join(Directory.GetCurrentDirectory(), "TestData", "configs");

    [ClassInitialize]
    public static void Initialize(TestContext testContext)
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
            .WithName("test-rabbitmq")
            .WithHostname("localhost")

            .WithResourceMapping(Path.Combine(CertsDirPath, "ca_certificate.crt"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(CertsDirPath, "server_certificate.pem"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(CertsDirPath, "server_key.pem"), "/etc/rabbitmq/certs")
            .WithResourceMapping(Path.Combine(ConfigsDirPath, "rabbitmq.conf"), "/etc/rabbitmq")
            .WithResourceMapping(Path.Combine(ConfigsDirPath, "enabled_plugins"), "/etc/rabbitmq")

            .WithEnvironment("RABBITMQ_DEFAULT_USER", "agent")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "agent123")

            .WithPortBinding(5672, 5672)
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
            "rabbitmqctl",
            "set_user_limits",
            "agent",
            "{\"max-connections\": 20}"
        });
    }

    [ClassCleanup]
    public static void BaseCleanup()
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
        }
    }

    protected static IContainer Container => rabbitContainer!;
}
