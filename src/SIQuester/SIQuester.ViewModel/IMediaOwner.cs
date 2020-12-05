using SIPackages.Core;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    public interface IMediaOwner
    {
        Task<IMedia> LoadMediaAsync();
    }
}
