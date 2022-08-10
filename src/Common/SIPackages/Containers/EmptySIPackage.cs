using SIPackages.Core;
using SIPackages.PlatformSpecific;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SIPackages.Containers
{
    /// <summary>
    /// Defines an empty SI package container.
    /// </summary>
    /// <inheritdoc cref="ISIPackage" />
    public sealed class EmptySIPackage : ISIPackage
    {
        /// <summary>
        /// Singleton empty SI package container.
        /// </summary>
        public static readonly EmptySIPackage Instance = new();

        /// <inheritdoc />
        public ISIPackage CopyTo(Stream stream, bool close, out bool isNew) => throw new NotImplementedException();

        /// <inheritdoc />
        public void CreateStream(string name, string contentType) => throw new NotImplementedException();

        /// <inheritdoc />
        public void CreateStream(string category, string name, string contentType) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task CreateStreamAsync(
            string category,
            string name,
            string contentType,
            Stream stream,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public void DeleteStream(string category, string name) => throw new NotImplementedException();

        /// <inheritdoc />
        public void Dispose() { }

        /// <inheritdoc />
        public void Flush() => throw new NotImplementedException();

        /// <inheritdoc />
        public string[] GetEntries(string category) => throw new NotImplementedException();

        /// <inheritdoc />
        public StreamInfo GetStream(string name, bool read = true) => throw new NotImplementedException();

        /// <inheritdoc />
        public StreamInfo GetStream(string category, string name, bool read = true) => throw new NotImplementedException();

        /// <inheritdoc />
        public long GetStreamLength(string name) => throw new NotImplementedException();

        /// <inheritdoc />
        public long GetStreamLength(string category, string name) => throw new NotImplementedException();
    }
}
