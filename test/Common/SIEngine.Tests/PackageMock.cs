using SIPackages.Core;
using SIPackages.PlatformSpecific;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIEngine.Tests
{
    internal sealed class PackageMock : ISIPackage
    {
        private readonly Dictionary<string, HashSet<string>> _streams = new();

        public ISIPackage CopyTo(Stream stream, bool close, out bool isNew)
        {
            throw new NotImplementedException();
        }

        public void CreateStream(string name, string contentType) =>
            CreateStream("", name, contentType);

        public void CreateStream(string category, string name, string contentType)
        {
            if (!_streams.TryGetValue(category, out var categoryStreams))
            {
                _streams[category] = categoryStreams = new HashSet<string>();
            }

            categoryStreams.Add(name);
        }

        public Task CreateStream(string category, string name, string contentType, Stream stream)
        {
            throw new NotImplementedException();
        }

        public void DeleteStream(string category, string name)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public string[] GetEntries(string category)
        {
            if (!_streams.TryGetValue(category, out var categoryStreams))
            {
                return Array.Empty<string>();
            }

            return categoryStreams.ToArray();
        }

        public StreamInfo GetStream(string name, bool read = true)
        {
            throw new NotImplementedException();
        }

        public StreamInfo GetStream(string category, string name, bool read = true)
        {
            throw new NotImplementedException();
        }
    }
}
