using SIPackages.Core;
using System;
using System.Collections.Generic;

namespace SICore
{
    /// <summary>
    /// Allows to share files by creating public access with Uris.
    /// </summary>
    public interface IShare : IDisposable
    {
        event Action<Exception> Error;

        string CreateUri(string file, byte[] data, string category);

        string CreateUri(string file, Func<StreamInfo> getStream, string category);

        string MakeUri(string file, string category);

        bool ContainsUri(string file);

        void StopUri(IEnumerable<string> toRemove);
    }
}
