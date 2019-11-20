using System;
using System.IO;
using System.Threading.Tasks;

namespace SImulator.ViewModel.PlatformSpecific
{
    public interface IPackageSource
    {
        string Name { get; }
        string Token { get; }

        Task<Stream> GetPackageAsync();
    }
}
