using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIPackages.Providers
{
    public interface IPackagesProvider
    {
        Task<IEnumerable<string>> GetPackages();
        Task<SIDocument> GetPackage(string name);
    }
}
