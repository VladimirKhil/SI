using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIPackages.Providers
{
    public interface IPackagesProvider
    {
        Task<IEnumerable<string>> GetPackagesAsync();
        Task<SIDocument> GetPackageAsync(string name);
    }
}
