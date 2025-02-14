using RabbitMQ.Client;
using System;

namespace Frends.RabbitMQ.Publish.Definitions;

/// <summary>
/// AMQP parameters.
/// </summary>
internal class RabbitMQChannel : IDisposable
{
    /// <summary>
    /// AMQP model parameters.
    /// </summary>
    public IChannel AMQPModel { get; set; } = null;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            AMQPModel?.CloseAsync().Wait();
            AMQPModel?.Dispose();
        }
    }
}