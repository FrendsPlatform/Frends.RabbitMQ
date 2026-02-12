using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.RabbitMQ.Publish.Definitions;

/// <summary>
/// Connection parameters.
/// </summary>
public class Connection
{
    /// <summary>
    /// Timeout setting for connection attempts. Value 0 indicates that the default value for the attempts should be used. Set value in seconds.
    /// </summary>
    /// <example>60</example>
    [DefaultValue(60)]
    public int Timeout { get; set; } = 60;

    /// <summary>
    /// Authentication method: URI, hostname with username/password, or certificate.
    /// </summary>
    /// <example>URI</example>
    public AuthenticationMethod AuthenticationMethod { get; set; }

    /// <summary>
    /// URI or hostname to connect to, depending of authentication method.
    /// </summary>
    /// <example>RabbitHost, amqp://foo:bar@localhost:1234</example>
    [PasswordPropertyText]
    public string Host { get; set; }

    /// <summary>
    /// Username to use when authenticating to the server.
    /// </summary>
    /// <example>foo</example>
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.Host)]
    [DisplayFormat(DataFormatString = "Text")]
    public string Username { get; set; } = "";

    /// <summary>
    /// Password to use when authenticating to the server.
    /// </summary>
    /// <example>bar</example>
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.Host)]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string Password { get; set; } = "";

    /// <summary>
    /// The port to connect on. Value 0 indicates that the default port for the protocol should be used.
    /// </summary>
    /// <example>0</example>
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.Host, AuthenticationMethod.Certificate)]
    public int Port { get; set; } = 0;

    /// <summary>
    /// Specifies the SSL protocol used for the RabbitMQ connection. Use SslProtocol.None to allow the operating system to negotiate the most secure protocol available.
    /// </summary>
    /// <example>SslProtocol.None</example>
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.Certificate)]
    public SslProtocol SslProtocol { get; set; }

    /// <summary>
    /// Specifies the source from which the client certificate should be loaded.
    /// </summary>
    /// <example>CertificateSource.File</example>
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.Certificate)]
    public CertificateSource CertificateSource { get; set; }

    /// <summary>
    /// Full file system path to a client certificate file (.pfx or .p12) used for TLS client‑certificate authentication.
    /// </summary>
    /// <example>C:\certs\client-auth.pfx</example>
    [UIHint(nameof(CertificateSource), "", CertificateSource.File)]
    public string ClientCertificatePath { get; set; }

    /// <summary>
    /// Password required to decrypt the certificate file. Needed for password‑protected .pfx/.p12 certificates.
    /// </summary>
    /// <example>MyStrongPassword123!</example>
    [UIHint(nameof(CertificateSource), "", CertificateSource.File, CertificateSource.RawBytes, CertificateSource.Base64)]
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string ClientCertificatePassword { get; set; }

    /// <summary>
    /// Base64‑encoded representation of a client certificate (.pfx or .p12).
    /// </summary>
    /// <example>MIIF2wIBAzCCB...</example>
    [UIHint(nameof(CertificateSource), "", CertificateSource.Base64)]
    [PasswordPropertyText]
    public string CertificateBase64 { get; set; }

    /// <summary>
    /// Raw byte array containing a client certificate (.pfx or .p12).
    /// </summary>
    /// <example>byte[] { 1, 2, 3, 4, 5 }</example>
    [UIHint(nameof(CertificateSource), "", CertificateSource.RawBytes)]
    [PasswordPropertyText]
    public byte[] CertificateBytes { get; set; }

    /// <summary>
    /// Thumbprint of a certificate stored in the Windows certificate store.
    /// </summary>
    /// <example>ab12cd34ef56ab78cd90ef12ab34cd56ef7890ab</example>
    [UIHint(nameof(CertificateSource), "", CertificateSource.Store)]
    public string StoreThumbprint { get; set; }

    /// <summary>
    /// Windows certificate store location to search when loading a certificate by thumbprint.
    /// </summary>
    /// <example>CertificateStoreLocation.CurrentUser</example>
    [UIHint(nameof(CertificateSource), "", CertificateSource.Store)]
    public CertificateStoreLocation CertificateStoreLocation { get; set; }

    /// <summary>
    /// The name of the queue. Leave empty to make the server generate a name.
    /// </summary>
    /// <example>SampleQueue</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string QueueName { get; set; }

    /// <summary>
    /// Exchange's name.
    /// </summary>
    /// <example>SampleExchange</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ExchangeName { get; set; } = "";

    /// <summary>
    /// Routing key's name.
    /// </summary>
    /// <example>route</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string RoutingKey { get; set; } = "";

    /// <summary>
    /// True to declare queue when publishing.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool Create { get; set; }

    /// <summary>
    /// Should this queue be auto-deleted when its last consumer (if any) unsubscribes?
    /// </summary>
    /// <example>false</example>
    [UIHint(nameof(Create), "", true)]
    [DefaultValue(false)]
    public bool AutoDelete { get; set; }

    /// <summary>
    /// Should this queue will survive a broker restart?
    /// Note that Quorum queue supports only Durable settings.
    /// </summary>
    /// <example>true</example>
    [UIHint(nameof(Create), "", true)]
    [DefaultValue(true)]
    public bool Durable { get; set; } = true;

    /// <summary>
    /// Should this queue be a quorum queue.
    /// </summary>
    /// <example>true</example>
    [UIHint(nameof(Create), "", true)]
    [DefaultValue(true)]
    public bool Quorum { get; set; } = true;

    /// <summary>
    /// Time in seconds how long a connection will be left open for reuse after the execution.
    /// </summary>
    /// <example>60</example>
    [DefaultValue(30)]
    public int ConnectionExpirationSeconds { get; set; } = 30;
}
