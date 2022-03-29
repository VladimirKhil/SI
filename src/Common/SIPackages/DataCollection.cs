using SIPackages.Core;
using SIPackages.PlatformSpecific;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIPackages
{
    /// <summary>
    /// Категория в хранилище
    /// </summary>
    /// <inheritdoc cref="IEnumerable{T}" />
    public sealed class DataCollection : IEnumerable<string>
    {
        private readonly string _mediaType;

        private ISIPackage _package = null;

        /// <summary>
        /// Существующие в коллекции файлы
        /// </summary>
        private readonly List<string> _files = null;

        /// <summary>
        /// Название коллекции
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Количество элементов в коллекции
        /// </summary>
        public int Count => _files.Count();

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
        /// Содержит ли категория файл
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>Содержит ли категория данный файл</returns>
        internal bool Contains(string fileName) => _files.Contains(fileName);

        /// <summary>
        /// Gets collection file.
        /// </summary>
        /// <param name="fileName">File name.</param>
        public StreamInfo GetFile(string fileName) => _package.GetStream(Name, fileName);

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
        /// Adss file to the collection.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="stream">File stream.</param>
        public async Task AddFileAsync(string fileName, Stream stream)
        {
            await _package.CreateStreamAsync(Name, fileName, _mediaType, stream);
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
        /// <returns></returns>
        public async Task RenameFileAsync(string oldName, string newName)
        {
            var streamInfo = _package.GetStream(Name, oldName);
            using (var stream = streamInfo.Stream)
            {
                await _package.CreateStreamAsync(Name, newName, _mediaType, stream);
            }

            _files.Add(newName);

            _package.DeleteStream(Name, oldName);
            _files.Remove(oldName);
        }

        internal void UpdateSource(ISIPackage package) => _package = package;
    }
}
