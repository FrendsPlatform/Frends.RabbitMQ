using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.RabbitMQ.Read.Definitions;

/// <summary>
/// RabbitMQ read parameters.
/// </summary>
public class Input
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
    /// The name of the queue.
    /// </summary>
    /// <example>SampleQueue</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string QueueName { get; set; }

    /// <summary>
    /// Exchange's name.
    /// </summary>
    /// <example>SampleExchange</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ExchangeName { get; set; } = string.Empty;

    /// <summary>
    /// Routing key's name.
    /// </summary>
    /// <example>route</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string RoutingKey { get; set; } = string.Empty;
}
