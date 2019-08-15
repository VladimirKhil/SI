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
        public int Count { get { return _files.Count(); } }

        /// <summary>
        /// Создание категории
        /// </summary>
        /// <param name="package">Документ-владелец</param>
        /// <param name="name">Имя категории</param>
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
        /// <param name="entryName">Имя файла</param>
        /// <returns>Содержит ли категория данный файл</returns>
        internal bool Contains(string fileName)
        {
            return _files.Contains(fileName);
        }

        public StreamInfo GetFile(string fileName)
        {
            return _package.GetStream(Name, fileName);
        }

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

        public async Task AddFile(string fileName, Stream stream)
        {
            await _package.CreateStream(Name, fileName, _mediaType, stream);
            _files.Add(fileName);
        }

        public void RemoveFile(string fileName)
        {
            _package.DeleteStream(Name, fileName);
            _files.Remove(fileName);
        }

        public async Task RenameFile(string oldName, string newName)
        {
            var streamInfo = _package.GetStream(Name, oldName);
            using (var stream = streamInfo.Stream)
            {
                await _package.CreateStream(Name, newName, _mediaType, stream);
            }

            _files.Add(newName);

            _package.DeleteStream(Name, oldName);
            _files.Remove(oldName);
        }

        internal void UpdateSource(ISIPackage package)
        {
            _package = package;
        }
    }
}
