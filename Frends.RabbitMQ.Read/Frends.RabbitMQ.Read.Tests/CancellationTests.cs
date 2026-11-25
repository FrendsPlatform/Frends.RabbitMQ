using Frends.RabbitMQ.Read.Definitions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Frends.RabbitMQ.Read.Tests;

[TestClass]
public class CancellationTests
{
    [TestMethod]
    public async Task ReadWithCanceledTokenReturnsFailureWhenThrowingDisabled()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var result = await RabbitMQ.Read(new Input(), new Options { ThrowErrorOnFailure = false }, source.Token);

        Assert.IsFalse(result.Success);
        Assert.IsInstanceOfType<OperationCanceledException>(result.Error.AdditionalInfo);
    }
}
