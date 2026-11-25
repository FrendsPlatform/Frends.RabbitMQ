namespace Frends.RabbitMQ.Read.Definitions;

/// <summary>
/// Message's data.
/// </summary>
public class Message
{
    /// <summary>
    /// Message.
    /// </summary>
    /// <example>Test message.</example>
    public string Data { get; set; }

    /// <summary>
    /// Message's header(s).
    /// </summary>
    /// <example>foo, bar</example>
    public Dictionary<string, string> Headers { get; set; }

    /// <summary>
    /// Message count.
    /// </summary>
    /// <example>1</example>
    public uint MessagesCount { get; set; }

    /// <summary>
    /// Message's delivery tag.
    /// </summary>
    /// <example>1</example>
    public ulong DeliveryTag { get; set; }
}
