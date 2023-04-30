namespace SI.GameServer.Contract;

/// <summary>
/// Contains public game package info.
/// </summary>
public sealed class PackageInfo
{
    /// <summary>
    /// Package type.
    /// </summary>
    public PackageType Type {  get; set; }

    /// <summary>
    /// Package relative or absolute uri.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Content service uri.
    /// </summary>
    /// <remarks>
    /// For <see cref="Type" /> of <see cref="PackageType.Content" /> this is the service that hosts the package.
    /// The uri is used when a well-known Content service is used. Game server will find out the package access secret from its configuration.
    /// For custom content service this parameter is optional.
    /// For <see cref="Type" /> of <see cref="PackageType.LibraryItem" /> this is the desired service to host the package.
    /// The package would be downloaded by this server and extracted.
    /// This parameter is optional in that case; Game server will pick up one of the available content services by itself.
    /// </remarks>
    public Uri? ContentServiceUri { get; set; }

    /// <summary>
    /// Secret used for accessing package root content file when a custom Content service is used.
    /// </summary>
    public string? Secret { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{Type}:{Uri}@{ContentServiceUri}:{Secret}";
}
