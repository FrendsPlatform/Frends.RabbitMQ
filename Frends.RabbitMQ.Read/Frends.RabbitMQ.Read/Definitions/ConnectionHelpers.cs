using System;
using RabbitMQ.Client;

namespace Frends.RabbitMQ.Read.Definitions;

/// <summary>
/// AMQP parameters.
/// </summary>
internal class ConnectionHelper : IDisposable
{
    /// <summary>
    /// AMQP connection parameters.
    /// </summary>
    /// <example>RabbitMQ connection to localhost</example>
    public IConnection AMQPConnection { get; set; } = null;

    /// <summary>
    /// AMQP model parameters.
    /// </summary>
    /// <example>Channel for reading messages from SampleQueue</example>
    public IChannel AMQPModel { get; set; } = null;

    /// <summary>
    /// Closes and disposes the RabbitMQ channel and connection.
    /// </summary>
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
            AMQPConnection?.CloseAsync().Wait();
            AMQPConnection?.Dispose();
        }
    }
}
