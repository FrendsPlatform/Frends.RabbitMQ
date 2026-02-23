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
/// Acknowledge type while reading message.
/// </summary>
public enum AckType
{
#pragma warning disable CS1591 // Self explanatory
    NoAck,
    AutoAck,
    AutoNack,
    AutoNackAndRequeue,
    AutoReject,
    AutoRejectAndRequeue,
#pragma warning restore CS1591 // Self explanatory
}
