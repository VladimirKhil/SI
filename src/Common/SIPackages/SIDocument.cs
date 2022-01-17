using SIPackages.Core;
using SIPackages.PlatformSpecific;
using SIPackages.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace SIPackages
{
    /// <summary>
    /// Документ SIGame
    /// </summary>
    public sealed class SIDocument : IDisposable
    {
        #region Fields

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ISIPackage _source;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Package _package;

        /// <summary>
        /// Коллекция авторов
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AuthorInfoList _authors;

        /// <summary>
        /// Коллекция источников
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SourceInfoList _sources;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DataCollection _images;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DataCollection _audio;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DataCollection _video;

        #endregion

        #region Static

        private const string ContentFileName = "content.xml";
        private const string AuthorsFileName = "authors.xml";
        private const string SourcesFileName = "sources.xml";

        /// <summary>
        /// Хранилище текстов
        /// </summary>
        private const string TextsStorageName = "Texts";
        /// <summary>
        /// Хранилище изображений
        /// </summary>
        public const string ImagesStorageName = "Images";
        /// <summary>
        /// Хранилище звуков
        /// </summary>
        public const string AudioStorageName = "Audio";
        /// <summary>
        /// Хранилище видео
        /// </summary>
        public const string VideoStorageName = "Video";

        #endregion

        #region Properties

        /// <summary>
        /// Игровой пакет
        /// </summary>
        public Package Package { get { return _package; } }

        /// <summary>
        /// Коллекция авторов
        /// </summary>
        public List<AuthorInfo> Authors { get { return _authors; } }

        /// <summary>
        /// Коллекция источников
        /// </summary>
        public List<SourceInfo> Sources { get { return _sources; } }

        public DataCollection Images { get { return _images; } }
        public DataCollection Audio { get { return _audio; } }
        public DataCollection Video { get { return _video; } }

        #endregion

        #region Document Functions

        private SIDocument()
        {
            
        }

        public static SIDocument Create(string name, string author)
        {
            var document = new SIDocument();

            var ms = new MemoryStream();
            document.CreateInternal(ms, name, author, false);

            Init(document);

            return document;
        }

        public static SIDocument Create(
            string name,
            string author,
            Stream stream,
            bool leaveStreamOpen = false)
        {
            var document = new SIDocument();

            var ms = stream ?? new MemoryStream();
            document.CreateInternal(ms, name, author, leaveStreamOpen);

            Init(document);

            return document;
        }

        public static SIDocument Create(string name, string author, string folder)
        {
            var document = new SIDocument();

            document.CreateInternal(folder, name, author);

            Init(document);

            return document;
        }

        public static SIDocument Create(string name, string author, ISIPackage source)
        {
            var document = new SIDocument();

            document.CreateInternal(source, name, author);

            Init(document);

            return document;
        }

        private static void Init(SIDocument document)
        {
            document._package.ID = Guid.NewGuid().ToString();
            document._package.Date = DateTime.UtcNow.ToString("dd.MM.yyyy");
        }

        private void CreateInternal(Stream stream, string name, string author, bool leaveStreamOpen)
        {
            _source = SIPackageFactory.Instance.CreatePackage(stream, leaveStreamOpen);

            CreateCore(name, author);
        }

        private void CreateInternal(string folder, string name, string author)
        {
            _source = SIPackageFactory.Instance.CreatePackage(folder);

            CreateCore(name, author);
        }

        private void CreateInternal(ISIPackage source, string name, string author)
        {
            _source = source;

            CreateCore(name, author);
        }

        private void CreateCore(string name, string author)
        {
            _source.CreateStream(ContentFileName, "si/xml");
            _source.CreateStream(TextsStorageName, AuthorsFileName, "si/xml");
            _source.CreateStream(TextsStorageName, SourcesFileName, "si/xml");

            InitializeStorages();

            _package = new Package { Name = name };
            _package.Info.Authors.Add(author);

            _authors = new AuthorInfoList();
            _sources = new SourceInfoList();
        }

        public static SIDocument Load(Stream stream, bool read = true)
        {
            var document = new SIDocument();
            document.LoadInternal(stream, read);
            return document;
        }

        /// <summary>
        /// Загрузка пакета из папки
        /// </summary>
        /// <param name="folder">Папка с содержимым пакета</param>
        /// <returns>Загруженный пакет</returns>
        public static SIDocument Load(string folder)
        {
            var document = new SIDocument();
            document.LoadInternal(folder, true);
            return document;
        }

        public static SIDocument LoadXml(Stream stream)
        {
            var document = new SIDocument();

            var ms = new MemoryStream();
            document.CreateInternal(ms, "", "", false);

            using (var reader = XmlReader.Create(stream))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "package")
                    {
                        document._package = new Package();
                        document._package.ReadXml(reader);
                        break;
                    }
                }
            }

            return document;
        }

        private void LoadInternal(Stream stream, bool read)
        {
            _source = SIPackageFactory.Instance.GetPackage(stream, read);

            LoadData();
            InitializeStorages();
        }

        private void LoadInternal(string folder, bool read)
        {
            _source = SIPackageFactory.Instance.GetPackage(folder, read);

            LoadData();
            InitializeStorages();
        }

        private void LoadData()
        {
            var streamInfo = _source.GetStream(ContentFileName);
            if (streamInfo != null)
            {
                using (streamInfo.Stream)
                {
                    using var reader = XmlReader.Create(streamInfo.Stream);
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "package")
                        {
                            _package = new Package();
                            _package.ReadXml(reader);
                            break;
                        }
                    }
                }
            }

            streamInfo = _source.GetStream(TextsStorageName, AuthorsFileName);
            if (streamInfo != null)
            {
                using (streamInfo.Stream)
                {
                    using var reader = XmlReader.Create(streamInfo.Stream);
                    _authors = new AuthorInfoList();
                    _authors.ReadXml(reader);
                }
            }

            streamInfo = _source.GetStream(TextsStorageName, SourcesFileName);
            if (streamInfo != null)
            {
                using (streamInfo.Stream)
                {
                    using var reader = XmlReader.Create(streamInfo.Stream);
                    _sources = new SourceInfoList();
                    _sources.ReadXml(reader);
                }
            }
        }

        private void InitializeStorages()
        {
            _images = new DataCollection(_source, ImagesStorageName, "si/image");
            _audio = new DataCollection(_source, AudioStorageName, "si/audio");
            _video = new DataCollection(_source, VideoStorageName, "si/video");
        }

        public void Save()
        {
            SaveCore(_source);
        }

        private void SaveCore(ISIPackage package)
        {
            using (var stream = package.GetStream(ContentFileName, false).Stream)
            {
                using var writer = XmlWriter.Create(stream);
                _package.WriteXml(writer);
            }

            using (var stream = package.GetStream(TextsStorageName, AuthorsFileName, false).Stream)
            {
                using var writer = XmlWriter.Create(stream);
                _authors.WriteXml(writer);
            }

            using (var stream = package.GetStream(TextsStorageName, SourcesFileName, false).Stream)
            {
                using var writer = XmlWriter.Create(stream);
                _sources.WriteXml(writer);
            }

            package.Flush();
        }

        public SIDocument SaveAs(Stream stream, bool switchTo)
        {
            var newSource = _source.CopyTo(stream, switchTo, out bool isNew);

            if (isNew)
            {
                newSource.CreateStream(ContentFileName, "si/xml");
                newSource.CreateStream(TextsStorageName, AuthorsFileName, "si/xml");
                newSource.CreateStream(TextsStorageName, SourcesFileName, "si/xml");
            }
            
            if (switchTo)
            {
                _source.Dispose();
                _source = newSource;

                _images.UpdateSource(_source);
                _audio.UpdateSource(_source);
                _video.UpdateSource(_source);
            }

            SaveCore(newSource);

            if (switchTo)
            {
                return this;
            }

            var doc = new SIDocument { _source = newSource };
            doc.InitializeStorages();

            return doc;
        }

        public void SaveXml(Stream stream)
        {
            var package = _package.Clone();
            InsertLinkValue(package);
            foreach (var round in package.Rounds)
            {
                InsertLinkValue(round);
                foreach (var theme in round.Themes)
                {
                    InsertLinkValue(theme);
                    foreach (var quest in theme.Questions)
                    {
                        InsertLinkValue(quest);
                        foreach (var atom in quest.Scenario)
                        {
                            if (atom.Text.ExtractLink().Length > 0)
                                atom.Text = string.Format("{0} {1}", Resources.LinkMissed, atom.Text);
                        }
                    }
                }
            }

            using var writer = XmlWriter.Create(stream);
            package.WriteXml(writer);
        }

        /// <summary>
        /// Заменить ссылки на их значения
        /// </summary>
        /// <param name="item">Объект, для которого проводится замена</param>
        private void InsertLinkValue(InfoOwner item)
        {
            for (int i = 0; i < item.Info.Authors.Count; i++)
            {
                var author = GetLink(item.Info.Authors, i);
                if (author != null)
                {
                    item.Info.Authors[i].ExtractLink(out string tail);
                    item.Info.Authors[i] = author.ToString() + tail;
                }
            }

            for (int i = 0; i < item.Info.Sources.Count; i++)
            {
                var source = GetLink(item.Info.Sources, i);
                if (source != null)
                {
                    item.Info.Sources[i].ExtractLink(out string tail);
                    item.Info.Sources[i] = source.ToString() + tail;
                }
            }
        }

        #endregion

        #region Link Functions

        /// <summary>
        /// Выделить настоящих авторов
        /// </summary>
        /// <param name="authors">Список авторов</param>
        /// <returns>Набор авторов, где вычислены все ссылки</returns>
        public string[] GetRealAuthors(Authors authors)
        {
            var result = new List<string>();
            for (int i = 0; i < authors.Count; i++)
            {
                var author = GetLink(authors, i, out var tail);
                result.Add(author == null ? authors[i] : author.ToString() + tail);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Выделить настоящие источники
        /// </summary>
        /// <param name="authors">Список источников</param>
        /// <returns>Набор источников, где вычислены все ссылки</returns>
        public string[] GetRealSources(Sources sources)
        {
            var result = new List<string>();
            for (var i = 0; i < sources.Count; i++)
            {
                var source = GetLink(sources, i, out var tail);
                result.Add(source == null ? sources[i] : source.ToString() + tail);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Получить ссылку на автора из хранилища
        /// </summary>
        /// <param name="authors">Список авторов</param>
        /// <param name="index">Индекс в списке авторов</param>
        /// <returns>Автор из хранилища</returns>
        public AuthorInfo GetLink(Authors authors, int index)
        {
            var link = authors[index].ExtractLink();
            return _authors.FirstOrDefault(author => author.Id == link);
        }

        /// <summary>
        /// Получить ссылку на автора из хранилища
        /// </summary>
        /// <param name="authors">Список авторов</param>
        /// <param name="index">Индекс в списке авторов</param>
        /// <returns>Автор из хранилища</returns>
        public AuthorInfo GetLink(Authors authors, int index, out string tail)
        {
            var link = authors[index].ExtractLink(out tail);
            return _authors.FirstOrDefault(author => author.Id == link);
        }

        /// <summary>
        /// Получить ссылку на источник из хранилища
        /// </summary>
        /// <param name="sources">Список источников</param>
        /// <param name="index">Индекс в списке источников</param>
        /// <returns>Источник из хранилища</returns>
        public SourceInfo GetLink(Sources sources, int index)
        {
            var link = sources[index].ExtractLink(true);
            return _sources.FirstOrDefault(source => source.Id == link);
        }

        /// <summary>
        /// Получить ссылку на источник из хранилища
        /// </summary>
        /// <param name="sources">Список источников</param>
        /// <param name="index">Индекс в списке источников</param>
        /// <returns>Источник из хранилища</returns>
        public SourceInfo GetLink(Sources sources, int index, out string tail)
        {
            var link = sources[index].ExtractLink(out tail);
            return _sources.FirstOrDefault(source => source.Id == link);
        }

        /// <summary>
        /// Получить ссылку на ресурс единицы сценария
        /// </summary>
        /// <param name="atom">Единица сценария</param>
        /// <returns>Ресурс, на который ссылкается данная единица</returns>
        public IMedia GetLink(Atom atom)
        {
            var link = atom.Text.ExtractLink();
            if (string.IsNullOrEmpty(link))
                return new Media(atom.Text);

            var collection = _images;
            switch (atom.Type)
            {
                case AtomTypes.Audio:
                    collection = _audio;
                    break;

                case AtomTypes.Video:
                    collection = _video;
                    break;
            }

            // TODO: сделать детерминированный выбор

            if (collection.Contains(link))
                return new Media(() => collection.GetFile(link), link);

            var escapedLink = Uri.EscapeUriString(link);

            if (collection.Contains(escapedLink))
                return new Media(() => collection.GetFile(escapedLink), escapedLink);

            var hash = ZipHelper.CalculateHash(link);

            if (collection.Contains(hash))
                return new Media(() => collection.GetFile(hash), hash);

            var escapedHash = ZipHelper.CalculateHash(escapedLink);

            if (collection.Contains(escapedHash))
                return new Media(() => collection.GetFile(escapedHash), escapedHash);

            // Это ссылка на внешний файл
            return new Media(link);
        }

        /// <summary>
        /// Связывание автора и хранилища
        /// </summary>
        /// <param name="authors">Коллекция авторов</param>
        /// <param name="index">Индекс в коллекции авторов</param>
        /// <param name="collectionIndex">Индекс автора в хранилище, с которым мы производим связывание</param>
        public void SetAuthorLink(IList<string> authors, int index, int collectionIndex)
        {
            authors[index] = "@" + Authors[collectionIndex].Id;
        }

        /// <summary>
        /// Связывание источника и хранилища
        /// </summary>
        /// <param name="sources">Коллекция источников</param>
        /// <param name="index">Индекс в коллекции источников</param>
        /// <param name="collectionIndex">Индекс источника в хранилище, с которым мы производим связывание</param>
        public void SetSourceLink(IList<string> sources, int index, int collectionIndex)
        {
            sources[index] = "@" + Sources[collectionIndex].Id;
        }

        /// <summary>
        /// Связывание единицы сценария и хранилища
        /// </summary>
        /// <param name="atom">Единица сценария</param>
        /// <param name="entryName">Название объекта в хранилище</param>
        public static void SetLink(Atom atom, string entryName)
        {
            atom.Text = "@" + entryName;
        }

        #endregion

        #region Collection functions

        public async Task CopyCollections(SIDocument newDocument)
        {
            CopyAuthorsAndSources(newDocument, Package);

            foreach (var round in Package.Rounds)
            {
                await CopyCollections(newDocument, round);
            }
        }

        public async Task CopyCollections(SIDocument newDocument, Round round)
        {
            CopyAuthorsAndSources(newDocument, round);

            foreach (var theme in round.Themes)
            {
                await CopyCollections(newDocument, theme);
            }
        }

        /// <summary>
        /// Скопировать все необходимые коллекции в новый документ, взяв за основу объект
        /// </summary>
        /// <param name="newDocument"></param>
        /// <param name="theme"></param>
        public async Task CopyCollections(SIDocument newDocument, Theme theme)
        {
            CopyAuthorsAndSources(newDocument, theme);

            foreach (var question in theme.Questions)
            {
                await CopyCollections(newDocument, question);
            }
        }

        public async Task CopyCollections(SIDocument newDocument, Question question)
        {
            CopyAuthorsAndSources(newDocument, question);

            foreach (var atom in question.Scenario)
            {
                await CopyMedia(newDocument, atom);
            }
        }

        private async Task CopyMedia(SIDocument newDocument, Atom atom)
        {
            var link = atom.Text.ExtractLink();

            var collection = Images;
            var newCollection = newDocument.Images;
            switch (atom.Type)
            {
                case AtomTypes.Audio:
                    collection = Audio;
                    newCollection = newDocument.Audio;
                    break;

                case AtomTypes.Video:
                    collection = Video;
                    newCollection = newDocument.Video;
                    break;
            }

            if (!newCollection.Contains(link))
            {
                if (collection.Contains(link))
                {
                    using (var stream = collection.GetFile(link).Stream)
                    {
                        await newCollection.AddFile(link, stream);
                    }
                }
            }
        }

        public void CopyAuthorsAndSources(SIDocument newDocument, InfoOwner infoOwner)
        {
            var length = infoOwner.Info.Authors.Count;
            for (int i = 0; i < length; i++)
            {
                var authorID = infoOwner.Info.Authors[i].ExtractLink();
                if (authorID.Length > 0)
                {
                    if (newDocument.Authors.All(author => author.Id != authorID))
                    {
                        var newAuthor = Authors.FirstOrDefault(author => author.Id == authorID);

                        if (newAuthor != null)
                        {
                            newDocument.Authors.Add(newAuthor.Clone());
                        }
                    }
                }
            }

            length = infoOwner.Info.Sources.Count;
            for (int i = 0; i < length; i++)
            {
                var sourceID = infoOwner.Info.Sources[i].ExtractLink(true);
                if (sourceID.Length > 0)
                {
                    if (newDocument.Sources.All(source => source.Id != sourceID))
                    {
                        var newSource = Sources.FirstOrDefault(source => source.Id == sourceID);

                        if (newSource != null)
                        {
                            newDocument.Sources.Add(newSource.Clone());
                        }
                    }
                }
            }
        }

        #endregion

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }

        public void FinalizeSave()
        {
            _source.Flush();
        }

        public void CopyData(SIDocument doc)
        {
            doc._package = _package;
            doc._authors = _authors;
            doc._sources = _sources;
        }

        public void ResetTo(Stream stream, bool read = true)
        {
            _source = SIPackageFactory.Instance.GetPackage(stream, read);

            _images.UpdateSource(_source);
            _audio.UpdateSource(_source);
            _video.UpdateSource(_source);
        }
    }
}
