using SIPackages.Core;
using SIPackages.PlatformSpecific;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SIPackages
{
    /// <summary>
    /// Defines a package files storage. All files belong to a single category.
    /// </summary>
    /// <inheritdoc cref="IEnumerable{T}" />
    public sealed class DataCollection : IEnumerable<string>
    {
        private readonly string _mediaType;

        private ISIPackage _package = null;

        /// <summary>
        /// Current items in the collection.
        /// </summary>
        private readonly List<string> _files = null;

        /// <summary>
        /// Collection name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Colletion item count.
        /// </summary>
        public int Count => _files.Count;

        /// <summary>
        /// Initilizes a new instance of <see cref="DataCollection" /> class.
        /// </summary>
        /// <param name="package">Package that owns the collection.</param>
        /// <param name="name">Collection name.</param>
        /// <param name="mediaType">Collection media type.</param>
        internal DataCollection(ISIPackage package, string name, string mediaType)
        {
            Name = name;
            _mediaType = mediaType;
            _package = package;

            _files = new List<string>(_package.GetEntries(Name));
        }

        /// <summary>
        /// Checks if the collection contains a file.
        /// </summary>
        /// <param name="fileName">File name.</param>
        internal bool Contains(string fileName) => _files.Contains(fileName);

        /// <summary>
        /// Gets collection file.
        /// </summary>
        /// <param name="fileName">File name.</param>
        public StreamInfo GetFile(string fileName) => _package.GetStream(Name, fileName);

        public long GetFileLength(string fileName) => _package.GetStreamLength(Name, fileName);

        #region IEnumerable<string> Members

        public IEnumerator<string> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Adds file to the collection.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="stream">File stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task AddFileAsync(string fileName, Stream stream, CancellationToken cancellationToken = default)
        {
            await _package.CreateStreamAsync(Name, fileName, _mediaType, stream, cancellationToken);
            _files.Add(fileName);
        }

        /// <summary>
        /// Removes file from the collection.
        /// </summary>
        /// <param name="fileName">File name.</param>
        public void RemoveFile(string fileName)
        {
            _package.DeleteStream(Name, fileName);
            _files.Remove(fileName);
        }

        /// <summary>
        /// Renames a file.
        /// </summary>
        /// <param name="oldName">Old file name.</param>
        /// <param name="newName">New file name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task RenameFileAsync(string oldName, string newName, CancellationToken cancellationToken = default)
        {
            var streamInfo = _package.GetStream(Name, oldName);
            using (var stream = streamInfo.Stream)
            {
                await _package.CreateStreamAsync(Name, newName, _mediaType, stream, cancellationToken);
            }

            _files.Add(newName);

            _package.DeleteStream(Name, oldName);
            _files.Remove(oldName);
        }

        internal void UpdateSource(ISIPackage package) => _package = package;
    }
}
