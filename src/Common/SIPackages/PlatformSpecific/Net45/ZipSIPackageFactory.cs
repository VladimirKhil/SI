using System;

namespace SIPackages.PlatformSpecific.Net45
{
    internal sealed class ZipSIPackageFactory : SIPackageFactory
    {
        public override ISIPackage CreatePackage(System.IO.Stream stream)
        {
            return ZipSIPackage.Create(stream);
        }

		public override ISIPackage CreatePackage(string folder)
		{
			return FolderSIPackage.Create(folder);
		}

		public override ISIPackage GetPackage(string folder, bool read = true)
        {
            return FolderSIPackage.Open(folder);
        }

        public override ISIPackage GetPackage(System.IO.Stream stream, bool read = true)
        {
            return ZipSIPackage.Open(stream, read);
        }
    }
}
