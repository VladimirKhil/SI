using System;
using System.IO;

namespace SIPackages.Core
{
    /// <summary>
    /// Обобщение мультимедиа-источника (URI или непосредственно поток).
    /// Удобно передавать в качестве параметра или результата функции.
    /// </summary>
    public sealed class Media: IMedia
    {
        public Func<StreamInfo> GetStream { get; }

        public string Uri { get; }

        public Media(Func<StreamInfo> getStream, string uri)
        {
            GetStream = getStream;
            Uri = uri;
        }

        public Media(string uri)
        {
            Uri = uri;
        }
    }
}
