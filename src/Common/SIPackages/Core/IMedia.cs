using System;
using System.IO;

namespace SIPackages.Core
{
    public interface IMedia
    {
        Func<StreamInfo> GetStream { get; }
        string Uri { get; }
    }
}
