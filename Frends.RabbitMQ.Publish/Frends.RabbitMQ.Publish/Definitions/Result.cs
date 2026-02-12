using System.Collections.Generic;

namespace Frends.RabbitMQ.Publish.Definitions;

/// <summary>
/// Publish result.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates if the task completed successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; private set; }

    /// <summary>
    /// Published data format.
    /// </summary>
    /// <example>String</example>
    public string DataFormat { get; private set; }

    /// <summary>
    /// Published data (message) in UTF8 string format.
    /// </summary>
    /// <example>Foo Bar</example>
    public string DataString { get; private set; }

    /// <summary>
    /// Published data (message) in byte array format.
    /// </summary>
    /// <example>46 6f 6f 20 42 61 72</example>
    public byte[] DataByteArray { get; private set; }

    /// <summary>
    /// Headers of the published message.
    /// </summary>
    /// <example>foo, bar</example>
    public Dictionary<string, string> Headers { get; private set; }

    internal Result(bool success, string dataFormat, string dataString, byte[] dataByteArray, Dictionary<string, string> headers)
    {
        Success = success;
        DataFormat = dataFormat;
        DataString = dataString;
        DataByteArray = dataByteArray;
        Headers = headers;
    }
}