using System.Collections.Generic;

namespace Frends.RabbitMQ.Read.Definitions;

/// <summary>
/// Read result(s).
/// </summary>
public class Result
{
    /// <summary>
    /// Read status. There was no messages to read if MessageUTF8 and MessagesBase64 are empty but Success=true.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Message in Base64 format.
    /// </summary>
    /// <example>VGVzdCBtZXNzYWdl, {foo, bar}, 1, 1</example>
    public List<Message> MessagesBase64 { get; private set; } = new List<Message>();

    /// <summary>
    /// Message in UTF8 format.
    /// </summary>
    /// <example>Test message, {foo, bar}, 1, 1</example>
    public List<Message> MessageUTF8 { get; private set; } = new List<Message>();

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }

    internal Result(bool success, List<Message> messagesBase64, List<Message> messageUTF8)
    {
        Success = success;
        MessagesBase64 = messagesBase64;
        MessageUTF8 = messageUTF8;
    }

    internal Result(bool success, Error error)
    {
        Success = success;
        Error = error;
    }
}
