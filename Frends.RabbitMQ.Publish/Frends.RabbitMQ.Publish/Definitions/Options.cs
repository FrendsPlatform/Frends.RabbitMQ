using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.RabbitMQ.Publish.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// Whether to throw an error on failure.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Overrides the error message on failure.
    /// </summary>
    /// <example>Custom error message</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;

    /// <summary>
    /// Internal channel settings from RabbitMQ Client
    /// </summary>
    /// <example>False</example>
    [DefaultValue(false)]
    public bool PublisherConfirmationsEnabled { get; set; } = false;

    /// <summary>
    /// Internal channel settings from RabbitMQ Client
    /// </summary>
    /// <example>False</example>
    [DefaultValue(false)]
    public bool PublisherConfirmationTrackingEnabled { get; set; } = false;
}
