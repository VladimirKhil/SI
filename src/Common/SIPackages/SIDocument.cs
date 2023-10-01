using EnsureThat;
using SIPackages.Containers;
using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Properties;
using System.Diagnostics;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Defines a SIGame document.
/// </summary>
public sealed class SIDocument : IDisposable
{
    internal const string ContentFileName = "content.xml";
    internal const string AuthorsFileName = "authors.xml";
    internal const string SourcesFileName = "sources.xml";

    private bool _disposed;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ISIPackageContainer _packageContainer;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Package _package = new();

    /// <summary>
    /// Коллекция авторов
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private AuthorInfoList _authors = new();

    /// <summary>
    /// Коллекция источников
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private SourceInfoList _sources = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DataCollection _images;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DataCollection _audio;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DataCollection _video;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DataCollection _html;

    /// <summary>
    /// Игровой пакет
    /// </summary>
    public Package Package => _package;

    /// <summary>
    /// Document authors collection.
    /// </summary>
    public List<AuthorInfo> Authors => Package.Version >= 5.0 ? Package.Global.Authors : _authors;

    /// <summary>
    /// Document sources collection.
    /// </summary>
    public List<SourceInfo> Sources => Package.Version >= 5.0 ? Package.Global.Sources : _sources;

    /// <summary>
    /// Document images collection.
    /// </summary>
    public DataCollection Images => _images;

    /// <summary>
    /// Document audio collection.
    /// </summary>
    public DataCollection Audio => _audio;

    /// <summary>
    /// Document video collection.
    /// </summary>
    public DataCollection Video => _video;

    /// <summary>
    /// Document HTML collection.
    /// </summary>
    public DataCollection Html => _html;

    #region Document Functions

    private SIDocument(ISIPackageContainer packageContainer)
    {
        Ensure.That(packageContainer).IsNotNull();

        _packageContainer = packageContainer;

        _images = new DataCollection(_packageContainer, CollectionNames.ImagesStorageName, "si/image");
        _audio = new DataCollection(_packageContainer, CollectionNames.AudioStorageName, "si/audio");
        _video = new DataCollection(_packageContainer, CollectionNames.VideoStorageName, "si/video");
        _html = new DataCollection(_packageContainer, CollectionNames.HtmlStorageName, "si/html");
    }

    /// <summary>
    /// Creates an empty document.
    /// </summary>
    /// <param name="name">Document name.</param>
    /// <param name="author">Document author.</param>
    public static SIDocument Create(string name, string author) => CreateInternal(new MemoryStream(), name, author, false);
        // CreateInternal(EmptySIPackageContainer.Instance, name, author);

    /// <summary>
    /// Creates a document and associates it with the stream.
    /// </summary>
    /// <param name="name">Document name.</param>
    /// <param name="author">Document author.</param>
    /// <param name="stream">Stream to write data.</param>
    /// <param name="leaveStreamOpen">Do not close the stream when disposing.</param>
    public static SIDocument Create(string name, string author, Stream stream, bool leaveStreamOpen = false) =>
        CreateInternal(stream, name, author, leaveStreamOpen);

    /// <summary>
    /// Creates a document and associates it with the folder.
    /// </summary>
    /// <param name="name">Document name.</param>
    /// <param name="author">Document author.</param>
    /// <param name="folder">Folder to write data.</param>
    public static SIDocument Create(string name, string author, string folder) => CreateInternal(folder, name, author);

    /// <summary>
    /// Creates a document and associates it with the specified source.
    /// </summary>
    /// <param name="name">Document name.</param>
    /// <param name="author">Document author.</param>
    /// <param name="packageContainer">Package container.</param>
    public static SIDocument Create(string name, string author, ISIPackageContainer packageContainer) =>
        CreateInternal(packageContainer, name, author);

    private static SIDocument CreateInternal(Stream stream, string name, string author, bool leaveStreamOpen) =>
        CreateInternal(PackageContainerFactory.CreatePackageContainer(stream, leaveStreamOpen), name, author);

    private static SIDocument CreateInternal(string folder, string name, string author) =>
        CreateInternal(PackageContainerFactory.CreatePackageContainer(folder), name, author);

    private static SIDocument CreateInternal(ISIPackageContainer packageContainer, string name, string author)
    {
        var document = new SIDocument(packageContainer);
        document.CreateCore(name, author);

        return document;
    }

    /// <summary>
    /// Creates new document from package.
    /// </summary>
    /// <param name="package">Document package.</param>
    public static SIDocument Create(Package package)
    {
        var stream = new MemoryStream();
        var packageContainer = PackageContainerFactory.CreatePackageContainer(stream);

        var document = new SIDocument(packageContainer);
        document.CreateCore(package);

        return document;
    }

    private void CreateCore(string name, string author)
    {
        _package.Name = name;
        _package.ID = Guid.NewGuid().ToString();
        _package.Date = DateTime.UtcNow.ToString("dd.MM.yyyy");
        _package.Info.Authors.Add(author);
    }

    private void CreateCore(Package package)
    {
        _package = package;
    }

    /// <summary>
    /// Loads document from stream.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="read">Should the document be read-only.</param>
    public static SIDocument Load(Stream stream, bool read = true) =>
        Load(PackageContainerFactory.GetPackageContainer(stream, read));

    /// <summary>
    /// Loads document from folder.
    /// </summary>
    /// <param name="folder">Source folder.</param>
    /// <param name="read">Should the document be read-only.</param>
    [Obsolete("Use ExtractToFolderAndLoadAsync")]
    public static SIDocument Load(string folder, bool read = true) =>
        Load(PackageContainerFactory.GetPackageContainer(folder, new Dictionary<string, string>()));

    /// <summary>
    /// Extracts document to folder and load from it.
    /// </summary>
    /// <param name="sourceFile">Source pakage file.</param>
    /// <param name="folder">Target folder.</param>
    /// <param name="maxAllowedDataLength">Maximum allowed length of extracted data in archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<SIDocument> ExtractToFolderAndLoadAsync(
        string sourceFile,
        string folder,
        long maxAllowedDataLength = long.MaxValue,
        CancellationToken cancellationToken = default)
    {
        var extractionMap = await PackageExtractor.ExtractPackageToFolderAsync(
            sourceFile,
            folder,
            maxAllowedDataLength,
            cancellationToken);

        return Load(PackageContainerFactory.GetPackageContainer(folder, extractionMap));
    }

    /// <summary>
    /// Loads document from stream as XML.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="packageContainer">Optional container to store package files.</param>
    public static SIDocument LoadXml(Stream stream, ISIPackageContainer? packageContainer = null)
    {
        packageContainer ??= EmptySIPackageContainer.Instance;
        var document = CreateInternal(packageContainer, "", "");

        using (var reader = XmlReader.Create(stream))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "package")
                {
                    document.Package.Info.Authors.Clear();
                    document.Package.ReadXml(reader);
                    break;
                }
            }
        }

        return document;
    }

    /// <summary>
    /// Loads document from custom package container.
    /// </summary>
    /// <param name="packageContainer">Custom package container.</param>
    public static SIDocument Load(ISIPackageContainer packageContainer)
    {
        var document = new SIDocument(packageContainer);
        document.LoadData();

        return document;
    }

    private void LoadData()
    {
        var streamInfo = _packageContainer.GetStream(ContentFileName);

        if (streamInfo != null)
        {
            using (streamInfo.Stream)
            {
                using var reader = XmlReader.Create(streamInfo.Stream);

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "package")
                    {
                        _package.ReadXml(reader);
                        break;
                    }
                }
            }
        }

        streamInfo = _packageContainer.GetStream(CollectionNames.TextsStorageName, AuthorsFileName);

        if (streamInfo != null)
        {
            using (streamInfo.Stream)
            {
                using var reader = XmlReader.Create(streamInfo.Stream);
                _authors.ReadXml(reader);
            }
        }

        streamInfo = _packageContainer.GetStream(CollectionNames.TextsStorageName, SourcesFileName);

        if (streamInfo != null)
        {
            using (streamInfo.Stream)
            {
                using var reader = XmlReader.Create(streamInfo.Stream);
                _sources.ReadXml(reader);
            }
        }
    }

    /// <summary>
    /// Upgrades document to new format.
    /// </summary>
    public bool Upgrade()
    {
        if (Package.Version >= 5.0)
        {
            return false;
        }

        foreach (var round in Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    question.Upgrade();
                }
            }
        }

        foreach (var author in _authors)
        {
            Package.Global.Authors.Add(author);
        }

        _authors.Clear();

        foreach (var source in _sources)
        {
            Package.Global.Sources.Add(source);
        }

        _sources.Clear();

        Package.Version = 5;
        return true;
    }

    /// <summary>
    /// Saves current document accepting all made changed.
    /// </summary>
    public void Save() => SaveCore(_packageContainer);

    private void SaveCore(ISIPackageContainer packageContainer)
    {
        using (var stream = CreateIfNotExists(packageContainer, ContentFileName).Stream)
        {
            using var writer = XmlWriter.Create(stream);
            _package.WriteXml(writer);
        }

        if (_authors.Any())
        {
            using var stream = CreateIfNotExists(packageContainer, CollectionNames.TextsStorageName, AuthorsFileName).Stream;
            using var writer = XmlWriter.Create(stream);
            _authors.WriteXml(writer);
        }

        if (_sources.Any())
        {
            using var stream = CreateIfNotExists(packageContainer, CollectionNames.TextsStorageName, SourcesFileName).Stream;
            using var writer = XmlWriter.Create(stream);
            _sources.WriteXml(writer);
        }

        packageContainer.Flush();
    }

    private static StreamInfo CreateIfNotExists(ISIPackageContainer packageContainer, string streamName)
    {
        var streamInfo = packageContainer.GetStream(streamName, false);

        if (streamInfo == null)
        {
            packageContainer.CreateStream(streamName, "si/xml");
            streamInfo = packageContainer.GetStream(streamName, false);

            if (streamInfo == null)
            {
                throw new InvalidOperationException($"Cannot create stream {streamName}");
            }
        }

        return streamInfo;
    }

    private static StreamInfo CreateIfNotExists(ISIPackageContainer packageContainer, string categoryName, string streamName)
    {
        var streamInfo = packageContainer.GetStream(categoryName, streamName, false);

        if (streamInfo == null)
        {
            packageContainer.CreateStream(categoryName, streamName, "si/xml");
            streamInfo = packageContainer.GetStream(categoryName, streamName, false);

            if (streamInfo == null)
            {
                throw new InvalidOperationException($"Cannot create stream {categoryName}/{streamName}");
            }
        }

        return streamInfo;
    }

    /// <summary>
    /// Saves document to stream.
    /// </summary>
    /// <param name="stream">Target stream.</param>
    /// <param name="switchTo">Should the document be retargeted to this stream.</param>
    /// <returns>Document based on new stream.</returns>
    public SIDocument SaveAs(Stream stream, bool switchTo)
    {
        Ensure.That(stream).IsNotNull();
        Ensure.That(_packageContainer).IsNotNull();
        Ensure.That(_images).IsNotNull();
        Ensure.That(_audio).IsNotNull();
        Ensure.That(_video).IsNotNull();
        Ensure.That(_html).IsNotNull();

        var newContainer = _packageContainer.CopyTo(stream, switchTo, out bool isNew);

        Ensure.That(newContainer).IsNotNull();

        if (isNew)
        {
            newContainer.CreateStream(ContentFileName, "si/xml");

            if (_authors.Any())
            {
                newContainer.CreateStream(CollectionNames.TextsStorageName, AuthorsFileName, "si/xml");
            }

            if (_sources.Any())
            {
                newContainer.CreateStream(CollectionNames.TextsStorageName, SourcesFileName, "si/xml");
            }
        }
        
        if (switchTo)
        {
            UpdateContainer(newContainer);
        }

        SaveCore(newContainer);

        if (switchTo)
        {
            return this;
        }

        return new SIDocument(newContainer);
    }

    /// <summary>
    /// Saves document XML to the stream.
    /// </summary>
    /// <param name="stream">Target stream.</param>
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
                        {
                            atom.Text = string.Format("{0} {1}", Resources.LinkMissed, atom.Text);
                        }
                    }
                }
            }
        }

        using var writer = XmlWriter.Create(stream);
        package.WriteXml(writer);
    }

    /// <summary>
    /// Replaces item links by linked values.
    /// </summary>
    /// <param name="item">Item which links are replaced.</param>
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
    /// <param name="sources">Список источников</param>
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
    public AuthorInfo? GetLink(Authors authors, int index)
    {
        var link = authors[index].ExtractLink();
        return _authors.FirstOrDefault(author => author.Id == link);
    }

    /// <summary>
    /// Получить ссылку на автора из хранилища
    /// </summary>
    /// <param name="authors">Список авторов</param>
    /// <param name="index">Индекс в списке авторов</param>
    /// <param name="tail">Author specification.</param>
    /// <returns>Автор из хранилища</returns>
    public AuthorInfo? GetLink(Authors authors, int index, out string tail)
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
    public SourceInfo? GetLink(Sources sources, int index)
    {
        var link = sources[index].ExtractLink(true);
        return _sources.FirstOrDefault(source => source.Id == link);
    }

    /// <summary>
    /// Получить ссылку на источник из хранилища
    /// </summary>
    /// <param name="sources">Список источников</param>
    /// <param name="index">Индекс в списке источников</param>
    /// <param name="tail">Link additional information.</param>
    /// <returns>Источник из хранилища</returns>
    public SourceInfo? GetLink(Sources sources, int index, out string tail)
    {
        var link = sources[index].ExtractLink(out tail);
        return _sources.FirstOrDefault(source => source.Id == link);
    }

    /// <summary>
    /// Gets link from atom text.
    /// </summary>
    /// <param name="atom">Atom containing the link.</param>
    /// <returns>Linked resource.</returns>
    public IMedia GetLink(Atom atom)
    {
        var link = atom.Text.ExtractLink();

        if (string.IsNullOrEmpty(link))
        {
            return new Media(atom.Text);
        }

        var collection = GetCollection(atom.Type);
        return GetLinkFromCollection(link, collection);
    }

    /// <summary>
    /// Gets link from content item.
    /// </summary>
    /// <param name="contentItem">Content item.</param>
    /// <returns>Linked resource.</returns>
    public IMedia GetLink(ContentItem contentItem)
    {
        var link = contentItem.Value;

        if (!contentItem.IsRef)
        {
            return new Media(link);
        }

        var collection = GetCollection(contentItem.Type);
        return GetLinkFromCollection(link, collection);
    }

    /// <summary>
    /// Tries to get media from content item.
    /// </summary>
    /// <param name="contentItem">Content item.</param>
    /// <returns>Media resource.</returns>
    public MediaInfo? TryGetMedia(ContentItem contentItem)
    {
        var link = contentItem.Value;

        if (!contentItem.IsRef)
        {
            return new MediaInfo(link);
        }

        var collectionName = CollectionNames.GetCollectionName(contentItem.Type);
        return TryGetMedia(collectionName, link);
    }

    /// <summary>
    /// Tries to get media collection by media type.
    /// </summary>
    /// <param name="mediaType">Collection media type.</param>
    /// <returns>Found collection or null.</returns>
    public DataCollection? TryGetCollection(string mediaType) => mediaType switch
    {
        AtomTypes.Image => _images,
        AtomTypes.Audio or AtomTypes.AudioNew => _audio,
        AtomTypes.Video => _video,
        AtomTypes.Html => _html,
        _ => null,
    };

    /// <summary>
    /// Gets media collection by media type.
    /// </summary>
    /// <param name="mediaType">Collection media type.</param>
    /// <exception cref="ArgumentException">Invalid media type has been provided.</exception>
    public DataCollection GetCollection(string mediaType) => TryGetCollection(mediaType)
        ?? throw new ArgumentException($"Invalid media type {mediaType}", nameof(mediaType));

    private static IMedia GetLinkFromCollection(string link, DataCollection collection)
    {
        // TODO: make deterministic choice

        if (collection.Contains(link))
        {
            return new Media(() => collection.GetFile(link), () => collection.GetFileLength(link), link);
        }

        var escapedLink = Uri.EscapeUriString(link);

        if (collection.Contains(escapedLink))
        {
            return new Media(() => collection.GetFile(escapedLink), () => collection.GetFileLength(escapedLink), escapedLink);
        }

        var hash = ZipHelper.CalculateHash(link);

        if (collection.Contains(hash))
        {
            return new Media(() => collection.GetFile(hash), () => collection.GetFileLength(hash), hash);
        }

        var escapedHash = ZipHelper.CalculateHash(escapedLink);

        if (collection.Contains(escapedHash))
        {
            return new Media(() => collection.GetFile(escapedHash), () => collection.GetFileLength(escapedHash), escapedHash);
        }

        // This is a link to an external resource
        return new Media(link);
    }

    private MediaInfo? TryGetMedia(string category, string link)
    {
        var media = _packageContainer.TryGetMedia(category, link);

        if (media.HasValue)
        {
            return media.Value;
        }

        var escapedLink = Uri.EscapeUriString(link);
        media = _packageContainer.TryGetMedia(category, escapedLink);

        if (media.HasValue)
        {
            return media.Value;
        }

        return null;
    }

    /// <summary>
    /// Связывание автора и хранилища
    /// </summary>
    /// <param name="authors">Коллекция авторов</param>
    /// <param name="index">Индекс в коллекции авторов</param>
    /// <param name="collectionIndex">Индекс автора в хранилище, с которым мы производим связывание</param>
    public void SetAuthorLink(IList<string> authors, int index, int collectionIndex)
    {
        authors[index] = $"@{Authors[collectionIndex].Id}";
    }

    /// <summary>
    /// Связывание источника и хранилища
    /// </summary>
    /// <param name="sources">Коллекция источников</param>
    /// <param name="index">Индекс в коллекции источников</param>
    /// <param name="collectionIndex">Индекс источника в хранилище, с которым мы производим связывание</param>
    public void SetSourceLink(IList<string> sources, int index, int collectionIndex)
    {
        sources[index] = $"@{Sources[collectionIndex].Id}";
    }

    /// <summary>
    /// Связывание единицы сценария и хранилища
    /// </summary>
    /// <param name="atom">Единица сценария</param>
    /// <param name="entryName">Название объекта в хранилище</param>
    public static void SetLink(Atom atom, string entryName)
    {
        atom.Text = $"@{entryName}";
    }

    #endregion

    #region Collection functions

    /// <summary>
    /// Copies all neccessary collections to the target document.
    /// </summary>
    /// <param name="newDocument">Target document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CopyCollectionsAsync(SIDocument newDocument, CancellationToken cancellationToken = default)
    {
        CopyAuthorsAndSources(newDocument, Package);

        foreach (var round in Package.Rounds)
        {
            await CopyCollectionsAsync(newDocument, round, cancellationToken);
        }
    }

    /// <summary>
    /// Copies all neccessary round collections to the target document.
    /// </summary>
    /// <param name="newDocument">Target document.</param>
    /// <param name="round">Source round.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CopyCollectionsAsync(SIDocument newDocument, Round round, CancellationToken cancellationToken = default)
    {
        CopyAuthorsAndSources(newDocument, round);

        foreach (var theme in round.Themes)
        {
            await CopyCollectionsAsync(newDocument, theme, cancellationToken);
        }
    }

    /// <summary>
    /// Скопировать все необходимые коллекции в новый документ, взяв за основу объект
    /// </summary>
    /// <param name="newDocument"></param>
    /// <param name="theme"></param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CopyCollectionsAsync(SIDocument newDocument, Theme theme, CancellationToken cancellationToken = default)
    {
        CopyAuthorsAndSources(newDocument, theme);

        foreach (var question in theme.Questions)
        {
            await CopyCollectionsAsync(newDocument, question, cancellationToken);
        }
    }

    /// <summary>
    /// Copies authors, sources and media files from question to the document.
    /// </summary>
    /// <param name="newDocument">Target document.</param>
    /// <param name="question">Original question.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CopyCollectionsAsync(SIDocument newDocument, Question question, CancellationToken cancellationToken = default)
    {
        CopyAuthorsAndSources(newDocument, question);

        foreach (var atom in question.Scenario)
        {
            await CopyMediaAsync(newDocument, atom, cancellationToken);
        }

        foreach (var contentItem in question.GetContent())
        {
            await CopyMediaAsync(newDocument, contentItem, cancellationToken);
        }
    }

    private async Task CopyMediaAsync(SIDocument newDocument, Atom atom, CancellationToken cancellationToken = default)
    {
        var link = atom.Text.ExtractLink();

        var collection = GetCollection(atom.Type);
        var newCollection = newDocument.GetCollection(atom.Type);

        if (!newCollection.Contains(link))
        {
            if (collection.Contains(link))
            {
                using var stream = collection.GetFile(link).Stream;
                await newCollection.AddFileAsync(link, stream, cancellationToken);
            }
        }
    }

    private async Task CopyMediaAsync(SIDocument newDocument, ContentItem contentItem, CancellationToken cancellationToken = default)
    {
        var link = contentItem.Value;

        var collection = GetCollection(contentItem.Type);
        var newCollection = newDocument.GetCollection(contentItem.Type);

        if (!newCollection.Contains(link))
        {
            if (collection.Contains(link))
            {
                var file = collection.GetFile(link);

                if (file != null)
                {
                    using var stream = file.Stream;
                    await newCollection.AddFileAsync(link, stream, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Copies object authors and sources to new document.
    /// </summary>
    /// <param name="newDocument">Target document.</param>
    /// <param name="infoOwner">Sorce object.</param>
    public void CopyAuthorsAndSources(SIDocument newDocument, InfoOwner infoOwner)
    {
        var length = infoOwner.Info.Authors.Count;

        for (var i = 0; i < length; i++)
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _packageContainer.Dispose();

        _disposed = true;
    }

    /// <summary>
    /// Flushes internal document source.
    /// </summary>
    public void FinalizeSave() => _packageContainer.Flush();

    /// <summary>
    /// Copies current document data to another document.
    /// </summary>
    /// <param name="doc">Target document.</param>
    public void CopyData(SIDocument doc)
    {
        doc._package = _package;
        doc._authors = _authors;
        doc._sources = _sources;
    }

    /// <summary>
    /// Switches document to the new source stream.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    public void ResetTo(Stream stream) => UpdateContainer(PackageContainerFactory.GetPackageContainer(stream));

    /// <summary>
    /// Switches document to new container disposing the old one.
    /// </summary>
    /// <param name="packageContainer">New container.</param>
    public void UpdateContainer(ISIPackageContainer packageContainer)
    {
        Ensure.That(packageContainer).IsNotNull();

        _packageContainer.Dispose();
        _packageContainer = packageContainer;

        _images.UpdateContainer(_packageContainer);
        _audio.UpdateContainer(_packageContainer);
        _video.UpdateContainer(_packageContainer);
        _html.UpdateContainer(_packageContainer);
    }
}
