namespace SIQuester.ViewModel.Configuration;

/// <summary>
/// Represents Chgk database client options.
/// </summary>
public sealed class ChgkDbClientOptions
{
    /// <summary>
    /// Options configuration section name.
    /// </summary>
    public static readonly string ConfigurationSectionName = "ChgkDbClient";

    /// <summary>
    /// Default retry count value.
    /// </summary>
    public const int DefaultRetryCount = 3;

    /// <summary>
    /// Chgk database service uri.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Retry count policy.
    /// </summary>
    public int RetryCount { get; set; } = DefaultRetryCount;

    /// <summary>
    /// Client timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
