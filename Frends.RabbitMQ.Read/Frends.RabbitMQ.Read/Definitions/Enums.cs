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
    CertificateWithCredentials,
}

/// <summary>
/// Certificate source options.
/// </summary>
public enum CertificateSource
{
    /// <summary>Load a certificate from a file.</summary>
    File,

    /// <summary>Load a certificate from the certificate store.</summary>
    Store,

    /// <summary>Load a Base64-encoded certificate.</summary>
    Base64,

    /// <summary>Load certificate bytes.</summary>
    RawBytes,
}

/// <summary>
/// SSL protocol options
/// </summary>
public enum SslProtocol
{
    /// <summary>Use the system default SSL protocol.</summary>
    None,

    /// <summary>Use TLS 1.2.</summary>
    Tls12,

    /// <summary>Use TLS 1.3.</summary>
    Tls13,
}

/// <summary>
/// Store location options
/// </summary>
public enum CertificateStoreLocation
{
    /// <summary>Search the local machine certificate store.</summary>
    LocalMachine,

    /// <summary>Search the current user's certificate store.</summary>
    CurrentUser,
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
