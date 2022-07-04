using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace SICore
{
    /// <summary>
    /// Класс, раздающий мультимедиа по некоторому URI
    /// </summary>
    public abstract class ShareBase : IShare
    {
        protected Dictionary<string, Func<StreamInfo>> _files = new Dictionary<string, Func<StreamInfo>>();
        protected object _filesSync = new object();

        public event Action<Exception> Error;

        public virtual string CreateURI(string file, byte[] data, string category)
        {
            lock (_filesSync)
            {
                if (ContainsURI(file))
                    return MakeURI(file, category);

                if (data.Length > 1000000)
                    return "";

                _files[file] = () => new StreamInfo { Stream = new MemoryStream(data), Length = data.Length };
            }

            return MakeURI(file, category);
        }

        public virtual string CreateURI(string file, Func<StreamInfo> getStream, string category)
        {
            lock (_filesSync)
            {
                if (ContainsURI(file))
                {
                    return MakeURI(file, category);
                }

                _files[file] = getStream;
            }

            return MakeURI(file, category);
        }

        public abstract string MakeURI(string file, string category);

        public bool ContainsURI(string file)
        {
            lock (_filesSync)
            {
                return _files.ContainsKey(file);
            }
        }

        public virtual void StopURI(IEnumerable<string> toRemove)
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
