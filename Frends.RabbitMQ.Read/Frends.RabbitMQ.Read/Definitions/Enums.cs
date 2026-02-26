namespace Frends.RabbitMQ.Read.Definitions;

/// <summary>
/// Authentication methods.
/// </summary>
public enum AuthenticationMethod
{
    /// <summary>
    /// Connect with URI.
    /// </summary>
    URI,

    /// <summary>
    /// Connect with hostname. Username and password are optional.
    /// </summary>
    Host
}

/// <summary>
/// Acknowledge type while reading a message.
/// </summary>
public enum AckType
{
    /// <summary>
    /// Ack message will not be sent
    /// </summary>
    NoAck,
    /// <summary>
    /// Ack message will be sent automatically
    /// </summary>
    AutoAck,
    /// <summary>
    /// Nack message will be sent automatically
    /// </summary>
    AutoNack,
    /// <summary>
    /// Nack message will be sent automatically and a message will be requeued
    /// </summary>
    AutoNackAndRequeue,
    /// <summary>
    /// Message will be automatically rejected
    /// </summary>
    AutoReject,
    /// <summary>
    /// Message will be automatically rejected and requeued
    /// </summary>
    AutoRejectAndRequeue,
}
