using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    public interface IMediaOwner
    {
        Task<IMedia> LoadMediaAsync();
    }
}
