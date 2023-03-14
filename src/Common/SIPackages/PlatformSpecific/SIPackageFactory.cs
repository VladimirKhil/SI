using SIPackages.Containers;

namespace SIPackages.PlatformSpecific;

/// <summary>
/// Provides helper methods for creating <see cref="ISIPackageContainer" /> instances.
/// </summary>
[Obsolete("Use PackageContainerFactory")]
internal abstract class SIPackageFactory
{
    internal static SIPackageFactory Instance = new Net45.ZipSIPackageFactory();

    public abstract ISIPackageContainer CreatePackage(Stream stream, bool leaveOpen = false);
    public abstract ISIPackageContainer CreatePackage(string folder);
    public abstract ISIPackageContainer GetPackage(Stream stream, bool read = true);
    public abstract ISIPackageContainer GetPackage(string folder, bool read = true);
}
