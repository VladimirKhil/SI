namespace SIPackages.PlatformSpecific.Net45
{
    internal sealed class ZipSIPackageFactory : SIPackageFactory
    {
        public override ISIPackage CreatePackage(System.IO.Stream stream, bool leaveOpen = false) =>
            ZipSIPackage.Create(stream, leaveOpen);

        public override ISIPackage CreatePackage(string folder) => FolderSIPackage.Create(folder);

        public override ISIPackage GetPackage(string folder, bool read = true) => FolderSIPackage.Open(folder);

        public override ISIPackage GetPackage(System.IO.Stream stream, bool read = true) => ZipSIPackage.Open(stream, read);
    }
}
