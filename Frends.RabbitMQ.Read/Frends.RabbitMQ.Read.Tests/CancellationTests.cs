using Frends.RabbitMQ.Read.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.RabbitMQ.Read.Tests;

[TestClass]
public class CancellationTests
{
    [TestMethod]
    public async Task ReadWithCanceledTokenThrowsEvenWhenThrowingDisabled()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => RabbitMQ.Read(
            new Input(),
            new Connection(),
            new Options { ThrowErrorOnFailure = false },
            source.Token));
    }
}
