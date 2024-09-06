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
    public int Timeout { get; set; }

    /// <summary>
    /// URI or hostname with username and password.
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
    [UIHint(nameof(AuthenticationMethod), "", AuthenticationMethod.Host)]
    public int Port { get; set; } = 0;

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
    public bool Durable { get; set; }

    /// <summary>
    /// Should this queue be a quorum queue.
    /// </summary>
    /// <example>true</example>
    [UIHint(nameof(Create), "", true)]
    [DefaultValue(true)]
    public bool Quorum { get; set; }

    /// <summary>
    /// Time in seconds how long a connection will be left open for reuse after the execution.
    /// </summary>
    /// <example>60</example>
    [DefaultValue(30)]
    public int ConnectionExpirationSeconds { get; set; }
}