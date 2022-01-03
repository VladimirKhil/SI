using System;

namespace SIPackages.Core
{
    public interface IMedia
    {
        Func<StreamInfo> GetStream { get; }
        string Uri { get; }
    }
}
