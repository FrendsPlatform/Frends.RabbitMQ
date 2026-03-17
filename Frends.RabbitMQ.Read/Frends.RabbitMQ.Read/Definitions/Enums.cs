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
    Host,

    /// <summary>
    /// Connect with certificate.
    /// </summary>
    Certificate,

    /// <summary>
    /// Connect with certificate and credentials.
    /// </summary>
    CertificateWithCredentials
}

/// <summary>
/// Certificate source options.
/// </summary>
public enum CertificateSource
{
#pragma warning disable CS1591 // self explanatory.
    File,
    Store,
    Base64,
    RawBytes
#pragma warning restore CS1591 // self explanatory.
}

/// <summary>
/// SSL protocol options
/// </summary>
public enum SslProtocol
{
#pragma warning disable CS1591 // self explanatory.
    None,
    Tls12,
    Tls13
#pragma warning restore CS1591 // self explanatory.
}

/// <summary>
/// Store location options
/// </summary>
public enum CertificateStoreLocation
{
#pragma warning disable CS1591 // self explanatory.
    LocalMachine,
    CurrentUser
#pragma warning restore CS1591 // self explanatory.
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
