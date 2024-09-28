namespace SIPackages.Exceptions;

/// <summary>
/// Defines an exception that is thrown when a package version is not supported.
/// </summary>
public sealed class UnsupportedPackageVersionException : Exception
{
    /// <summary>
    /// Actual package version.
    /// </summary>
    public double ActualVersion { get; }

    /// <summary>
    /// Maximum supported package version.
    /// </summary>
    public double MaximumSupportedVersion { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="UnsupportedPackageVersionException" /> class.
    /// </summary>
    /// <param name="actualVersion">Actual package version.</param>
    /// <param name="maximumSupportedVersion">Maximum supported package version.</param>
    public UnsupportedPackageVersionException(double actualVersion, double maximumSupportedVersion)
    {
        ActualVersion = actualVersion;
        MaximumSupportedVersion = maximumSupportedVersion;
    }
}
