using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.RabbitMQ.Read.Definitions;

/// <summary>
/// Connection and output parameters.
/// </summary>
public class Connection
{
    /// <summary>
    /// Maximum number of messages to read.
    /// </summary>
    /// <example>1</example>
    [DefaultValue(1)]
    public int ReadMessageCount { get; set; }

    /// <summary>
    /// Set the acknowledgement type.
    /// </summary>
    /// <example>AutoAck</example>
    [DefaultValue(AckType.AutoAck)]
    public AckType AckType { get; set; }

    /// <summary>
    /// URI or hostname with username and password.
    /// </summary>
    /// <example>URI</example>
    public AuthenticationMethod AuthenticationMethod { get; set; }

    /// <summary>
    /// URI or hostname to connect to, depending on authentication method.
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
    /// Timeout setting for connection attempts. Value 0 indicates that the default value for the attempts should be used. Set value in seconds.
    /// </summary>
    /// <example>60</example>
    public int Timeout { get; set; }
}
