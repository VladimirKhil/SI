using System.IO;

namespace SIPackages.PlatformSpecific.Net45
{
    /// <inheritdoc cref="SIPackageFactory" />
    internal sealed class ZipSIPackageFactory : SIPackageFactory
    {
        public override ISIPackage CreatePackage(Stream stream, bool leaveOpen = false) =>
            ZipSIPackage.Create(stream, leaveOpen);

        public override ISIPackage CreatePackage(string folder) => FolderSIPackage.Create(folder);

        public override ISIPackage GetPackage(string folder, bool read = true) => FolderSIPackage.Open(folder);

        public override ISIPackage GetPackage(Stream stream, bool read = true) => ZipSIPackage.Open(stream, read);
    }
}
