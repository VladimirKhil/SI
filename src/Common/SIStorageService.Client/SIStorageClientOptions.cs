namespace SIStorageService.Client;

/// <summary>
/// Provides options for <see cref="SIStorageServiceClient" />.
/// </summary>
internal sealed class SIStorageClientOptions
{
    public const string ConfigurationSectionName = "SIStorageServiceClient";

    /// <summary>
    /// SIStorageService address.
    /// </summary>
    public Uri? ServiceUri { get; set; }
}
