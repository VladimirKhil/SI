﻿using System.IO;

namespace SIPackages.PlatformSpecific
{
    internal abstract class SIPackageFactory
    {
        internal static SIPackageFactory Instance = new Net45.ZipSIPackageFactory();

        public abstract ISIPackage CreatePackage(Stream stream);
        public abstract ISIPackage CreatePackage(string folder);
        public abstract ISIPackage GetPackage(Stream stream, bool read = true);
        public abstract ISIPackage GetPackage(string folder, bool read = true);
    }
}
