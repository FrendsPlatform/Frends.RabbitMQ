using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.RabbitMQ.Publish.Tests;

public abstract class TestBase
{
    private static readonly object Lock = new();

    private static IContainer? rabbitContainer;
    private static int refCount;
    private static bool isInitialized;

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
            .WithImage("rabbitmq:3.9-management")
            .WithName("test-rabbitmq")
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "agent")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "agent123")
            .WithPortBinding(5672, 5672) // AMQP
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5672))
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
}
