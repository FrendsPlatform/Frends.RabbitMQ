using RabbitMQ.Client;
using System;

namespace Frends.RabbitMQ.Publish.Definitions;

/// <summary>
/// AMQP parameters.
/// </summary>
internal class RabbitMQConnection : IDisposable
{
    /// <summary>
    /// AMQP connection parameters.
    /// </summary>
    public IConnection AMQPConnection { get; set; } = null;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            AMQPConnection?.CloseAsync().Wait();
            AMQPConnection?.Dispose();
        }
    }
}