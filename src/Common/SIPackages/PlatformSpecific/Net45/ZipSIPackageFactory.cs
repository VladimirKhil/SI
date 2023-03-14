using SIPackages.Containers;

namespace SIPackages.PlatformSpecific.Net45;

/// <inheritdoc cref="SIPackageFactory" />
[Obsolete("Use PackageContainerFactory")]
internal sealed class ZipSIPackageFactory : SIPackageFactory
{
    public override ISIPackageContainer CreatePackage(Stream stream, bool leaveOpen = false) =>
        ZipSIPackageContainer.Create(stream, leaveOpen);

    public override ISIPackageContainer CreatePackage(string folder) => FolderSIPackageContainer.Create(folder);

    public override ISIPackageContainer GetPackage(string folder, bool read = true) => FolderSIPackageContainer.Open(folder);

    public override ISIPackageContainer GetPackage(Stream stream, bool read = true) => ZipSIPackageContainer.Open(stream, read);
}
