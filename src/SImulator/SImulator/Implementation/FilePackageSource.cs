using SImulator.ViewModel.PlatformSpecific;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SImulator.Implementation
{
    internal sealed class FilePackageSource: IPackageSource
    {
        public string Name { get; }

        public string Token => Name;

        public FilePackageSource(string path)
        {
            Name = path;
        }

        public Task<Stream> GetPackageAsync() => Task.FromResult((Stream)File.OpenRead(Name));

        public override string ToString() => Name;
    }
}
