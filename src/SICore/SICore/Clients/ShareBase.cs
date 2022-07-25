using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace SICore
{
    /// <inheritdoc cref="IShare" />
    public abstract class ShareBase : IShare
    {
        private const int MaxFileSize = 1_000_000;

        protected Dictionary<string, Func<StreamInfo>> _files = new();
        protected object _filesSync = new();

        public event Action<Exception> Error;

        public virtual string CreateUri(string file, byte[] data, string category)
        {
            lock (_filesSync)
            {
                if (ContainsUri(file))
                {
                    return MakeUri(file, category);
                }

                if (data.Length > MaxFileSize)
                {
                    return "";
                }

                _files[file] = () => new StreamInfo { Stream = new MemoryStream(data), Length = data.Length };
            }

            return MakeUri(file, category);
        }

        public virtual string CreateUri(string file, Func<StreamInfo> getStream, string category)
        {
            lock (_filesSync)
            {
                if (ContainsUri(file))
                {
                    return MakeUri(file, category);
                }

                _files[file] = getStream;
            }

            return MakeUri(file, category);
        }

        public abstract string MakeUri(string file, string category);

        public bool ContainsUri(string file)
        {
            lock (_filesSync)
            {
                return _files.ContainsKey(file);
            }
        }

        public virtual void StopUri(IEnumerable<string> toRemove)
        {
            lock (_filesSync)
            {
                foreach (var item in toRemove)
                {
                    _files.Remove(item);
                }
            }
        }

        protected void OnError(Exception exception)
        {
            Error?.Invoke(exception);
        }

        public virtual void Dispose()
        {
            
        }
    }
}
