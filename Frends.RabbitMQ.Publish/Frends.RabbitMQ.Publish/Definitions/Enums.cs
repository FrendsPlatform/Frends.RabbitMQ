namespace Frends.RabbitMQ.Publish.Definitions;

/// <summary>
/// Data input types.
/// </summary>
public enum InputType
{
#pragma warning disable CS1591 // self explanatory.
    String,
    ByteArray
#pragma warning restore CS1591 // self explanatory.
}

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
    Certificate
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
