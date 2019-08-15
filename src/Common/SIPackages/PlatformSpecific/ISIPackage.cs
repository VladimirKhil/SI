using SIPackages.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SIPackages.PlatformSpecific
{
    /// <summary>
    /// Абстрактный источник пакета
    /// </summary>
    public interface ISIPackage: IDisposable
    {
        string[] GetEntries(string category);

        StreamInfo GetStream(string name, bool read = true);
        StreamInfo GetStream(string category, string name, bool read = true);

        void CreateStream(string name, string contentType);
        void CreateStream(string category, string name, string contentType);
        Task CreateStream(string category, string name, string contentType, Stream stream);

        void DeleteStream(string category, string name);

        ISIPackage CopyTo(Stream stream, bool close, out bool isNew);
        void Flush();
    }
}
