using SIPackages.Core;
using System;
using System.Collections.Generic;

namespace SICore
{
    public interface IShare : IDisposable
    {
        event Action<Exception> Error;

        string CreateURI(string file, byte[] data, string category);
        string CreateURI(string file, Func<StreamInfo> getStream, string category);

        string MakeURI(string file, string category);

        bool ContainsURI(string file);

        void StopURI(IEnumerable<string> toRemove);
    }
}
