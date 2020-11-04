using System.Threading;
using System.Threading.Tasks;

namespace Services.SI
{
    public interface ISIStorageServiceClient
    {
        Task<string[]> GetPackagesByTagAsync(int? tagId = null, CancellationToken cancellationToken = default);
    }
}
