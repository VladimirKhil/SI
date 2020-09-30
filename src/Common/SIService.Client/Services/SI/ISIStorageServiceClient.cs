using System.Threading;
using System.Threading.Tasks;

namespace Services.SI
{
    interface ISIStorageServiceClient
    {
        Task<string[]> GetPackagesByTagAsync(int? tagId = null, CancellationToken cancellationToken = default);
    }
}
