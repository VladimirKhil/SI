using Microsoft.Extensions.Logging;
using SIPackages;
using SIPackages.Core;
using SIPackages.Models;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.Model;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a media storage view model.
/// </summary>
public sealed class MediaStorageViewModel : WorkspaceViewModel
{
    private readonly QDocument _document;

    private readonly int _internalId = Random.Shared.Next();

    /// <summary>
    /// Добавленные файлы
    /// </summary>
    private readonly List<MediaItemViewModel> _added = new();

    /// <summary>
    /// Удалённые файлы
    /// </summary>
    private readonly List<MediaItemViewModel> _removed = new();

    /// <summary>
    /// Переименованные файлы
    /// </summary>
    private readonly List<Tuple<string, string>> _renamed = new();

    /// <summary>
    /// Пути для файлов, ещё не загруженных в коллекцию (не закоммиченных)
    /// </summary>
    private readonly Dictionary<MediaItemViewModel, Tuple<string, FileStream>> _streams = new();
    private readonly Dictionary<MediaItemViewModel, Tuple<string, FileStream>> _removedStreams = new();

    private bool _blockFlag = false;

    private bool _hasPendingChanges = false;

    public bool HasPendingChanges
    {
        get => _hasPendingChanges;
        set
        {
            if (_hasPendingChanges != value)
            {
                _hasPendingChanges = value;
                OnPropertyChanged();

                if (_hasPendingChanges)
                {
                    HasChanged?.Invoke();
                }
            }
        }
    }

    internal event Action<IChange>? Changed;

    internal void OnChanged(IChange change) => Changed?.Invoke(change);

    /// <summary>
    /// Collection files.
    /// </summary>
    public ObservableCollection<MediaItemViewModel> Files { get; } = new();

    private MediaItemViewModel? _currentFile = null;

    /// <summary>
    /// Current selected file.
    /// </summary>
    public MediaItemViewModel? CurrentFile
    {
        get => _currentFile;
        set
        {
            if (_currentFile != value)
            {
                _currentFile = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand AddItem { get; private set; }

    public ICommand DeleteItem { get; private set; }

    /// <summary>
    /// Compresses media item.
    /// </summary>
    public ICommand CompressItem { get; private set; }

    /// <summary>
    /// Navigates to media item usage.
    /// </summary>
    public ICommand NavigateToUsage { get; }

    private readonly string _header;

    private readonly string _name;

    internal string Name => _name;

    public override string Header => _header;

    public event Action? HasChanged;

    private readonly ILogger<MediaStorageViewModel> _logger;

    private string _filter = "";

    /// <summary>
    /// Storage files names filter.
    /// </summary>
    public string Filter
    {
        get => _filter;
        set
        {
            if (_filter != value)
            {
                _filter = value;
                OnPropertyChanged();
            }
        }
    }

    public MediaStorageViewModel(QDocument document, DataCollection collection, string header, ILogger<MediaStorageViewModel> logger, bool canCompress = false)
    {
        _document = document;
        _header = header;
        _name = collection.Name;
        _logger = logger;

        FillFiles(collection);

        AddItem = new SimpleCommand(AddItem_Executed);
        DeleteItem = new SimpleCommand(Delete_Executed);
        CompressItem = new SimpleCommand(CompressItem_Executed) { CanBeExecuted = canCompress };
        NavigateToUsage = new SimpleCommand(NavigateToUsage_Executed);
    }

    private void FillFiles(DataCollection collection)
    {
        foreach (var item in collection)
        {
            var named = CreateItem(item);
            Files.Add(named);
        }
    }

    private MediaItemViewModel CreateItem(string item)
    {
        var named = new MediaItemViewModel(new Named(item), _name, () => Wrap(item));
        named.Model.PropertyChanged += Named_PropertyChanged;
        return named;
    }

    private void Named_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_blockFlag || sender == null)
        {
            return;
        }

        var ext = (ExtendedPropertyChangedEventArgs<string>)e;
        var item = (Named)sender;

        if (Files.Count(obj => obj.Model.Name == item.Name) > 1)
        {
            SafeRename(item, ext.OldValue);
            return;
        }

        var newValue = item.Name;
        var renamedExisting = !_added.Any(mi => mi.Model == item);
        Tuple<string, string>? tuple = null;

        if (renamedExisting)
        {
            tuple = Tuple.Create(ext.OldValue, item.Name.Replace("%", ""));
            _renamed.Add(tuple);
        }

        var contentType = CollectionNames.TryGetContentType(_name) ?? _name;
        _document.RenameContentReference(contentType, ext.OldValue, item.Name);

        OnChanged(new CustomChange(
            () =>
            {
                if (renamedExisting)
                {
                    var found = false;

                    for (int i = _renamed.Count - 1; i >= 0; i--)
                    {
                        if (_renamed[i] == tuple)
                        {
                            _renamed.RemoveAt(i);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        _renamed.Add(Tuple.Create(item.Name, ext.OldValue));
                        HasPendingChanges = IsChanged();
                    }
                }

                SafeRename(item, ext.OldValue);
                _document.RenameContentReference(contentType, item.Name, ext.OldValue);
            },
            () =>
            {
                SafeRename(item, newValue);
                _document.RenameContentReference(contentType, ext.OldValue, item.Name);

                if (renamedExisting && tuple != null)
                {
                    var last = _renamed.LastOrDefault();

                    if (last != null && last.Item1 == tuple.Item2 && last.Item2 == tuple.Item1)
                    {
                        _renamed.RemoveAt(_renamed.Count - 1);
                    }
                    else
                    {
                        _renamed.Add(tuple);
                    }

                    HasPendingChanges = IsChanged();
                }
            }));
        
        HasPendingChanges = IsChanged();
    }

    private void SafeRename(Named item, string name)
    {
        _blockFlag = true;

        try
        {
            item.Name = name;
        }
        finally
        {
            _blockFlag = false;
        }
    }

    private bool IsChanged() => _added.Count > 0 || _removed.Count > 0 || _renamed.Count > 0;

    private void Delete_Executed(object? arg)
    {
        if (arg == null)
        {
            return;
        }

        try
        {
            var item = (MediaItemViewModel)arg;
            PreviewRemove(item);

            OnChanged(new CustomChange(
                () =>
                {
                    if (_removed.Contains(item))
                    {
                        _removed.Remove(item);
                    }
                    else if (_removedStreams.ContainsKey(item))
                    {
                        _added.Add(item);
                        _streams.Add(item, _removedStreams[item]);
                        _removedStreams.Remove(item);
                    }
                    else
                    {
                        return; // file was removed and removal has been committed
                    }

                    Files.Add(item);
                    OnPropertyChanged(nameof(Files));

                    HasPendingChanges = IsChanged();
                },
                () =>
                {
                    PreviewRemove(item);
                    HasPendingChanges = IsChanged();
                }));

            HasPendingChanges = IsChanged();
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void CompressItem_Executed(object? arg)
    {
        if (arg == null)
        {
            return;
        }

        var item = (MediaItemViewModel)arg;

        if (item.MediaSource == null)
        {
            return;
        }

        try
        {
            var sourceUri = item.MediaSource.Uri;
            var newUri = PlatformManager.Instance.CompressImage(sourceUri);

            if (newUri != sourceUri)
            {
                var newItem = CreateItem(item.Name);
                var currentIndex = Files.IndexOf(item);

                var returnToCurrent = CurrentFile == item;

                PreviewRemove(item);
                PreviewAdd(newItem, newUri, currentIndex);
                HasPendingChanges = IsChanged();

                if (returnToCurrent)
                {
                    CurrentFile = newItem;
                }

                OnChanged(new CustomChange(
                    () =>
                    {
                        var returnToCurrent = CurrentFile == newItem;

                        PreviewRemove(newItem);
                        PreviewAdd(item, sourceUri);
                        HasPendingChanges = IsChanged();

                        if (returnToCurrent)
                        {
                            CurrentFile = item;
                        }
                    },
                    () =>
                    {
                        var returnToCurrent = CurrentFile == item;

                        PreviewRemove(item);
                        PreviewAdd(newItem, newUri);
                        HasPendingChanges = IsChanged();

                        if (returnToCurrent)
                        {
                            CurrentFile = newItem;
                        }
                    }));
            }
        }
        catch (Exception ex)
        {
            OnError(ex);
        }
    }

    private void NavigateToUsage_Executed(object? arg)
    {
        if (arg == null)
        {
            return;
        }

        var item = (MediaItemViewModel)arg;
        var logo = _document.Package.Model.LogoItem;

        if (logo != null && item.Name == logo.Value)
        {
            _document.Navigate.Execute(_document.Package);
            return;
        }

        foreach (var round in _document.Package.Rounds)
        {
            foreach (var theme in round.Themes)
            {
                foreach (var question in theme.Questions)
                {
                    foreach (var content in question.Model.GetContent())
                    {
                        if (content.Value == item.Name)
                        {
                            _document.Navigate.Execute(question);
                            return;
                        }
                    }
                }
            }
        }
    }

    private void PreviewRemove(MediaItemViewModel item)
    {
        if (!Files.Contains(item))
        {
            return;
        }

        if (_added.Contains(item))
        {
            _added.Remove(item);
            _removedStreams.Add(item, _streams[item]);
            _streams.Remove(item);
        }
        else
        {
            _removed.Add(item);
        }

        Files.Remove(item);
        OnPropertyChanged(nameof(Files));
    }

    public async Task CommitAsync(DataCollection collection, CancellationToken cancellationToken = default)
    {
        foreach (var item in _removed.ToArray())
        {
            collection.RemoveFile(item.Model.Name);
            item.PropertyChanged -= Named_PropertyChanged;
            _removed.Remove(item);
        }

        foreach (var item in _removedStreams)
        {
            item.Value.Item2.Dispose();
            _removedStreams.Remove(item.Key);
        }

        foreach (var item in _added.ToArray())
        {
            try
            {
                using var fs = _streams[item].Item2;
                await collection.AddFileAsync(item.Model.Name, fs, cancellationToken);
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
            
            _added.Remove(item);
            _streams.Remove(item);
        }

        foreach (var item in _renamed.ToArray())
        {
            await collection.RenameFileAsync(item.Item1, item.Item2, cancellationToken);
            _renamed.Remove(item);
        }

        HasPendingChanges = false;
    }

    public async Task ApplyToAsync(DataCollection collection, bool final = false)
    {
        foreach (var item in _removed.ToArray())
        {
            collection.RemoveFile(item.Model.Name);
        }

        foreach (var item in _added.ToArray())
        {
            var fs = _streams[item].Item2;

            if (final)
            {
                using (fs)
                {
                    await collection.AddFileAsync(item.Model.Name, fs);
                }
            }
            else
            {
                await collection.AddFileAsync(item.Model.Name, fs);
                fs.Position = 0;
            }
        }

        foreach (var item in _renamed.ToArray())
        {
            await collection.RenameFileAsync(item.Item1, item.Item2);
        }

        if (final)
        {
            _added.Clear();
            _removed.Clear();
            _renamed.Clear();
        }
    }

    private void AddItem_Executed(object? arg)
    {
        var multiselect = arg is not bool b || b;
        var files = PlatformManager.Instance.ShowMediaOpenUI(_name, !_document.Package.HasQualityControl, multiselect);

        if (files == null)
        {
            return;
        }

        foreach (var file in files)
        {
            try
            {
                AddFile(file);
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        HasPendingChanges = IsChanged();
    }

    public MediaItemViewModel AddFile(string file, string? name = null)
    {
        if (_document.Package.HasQualityControl)
        {
            ValidateFileExtensionAndSize(file);
        }

        var localName = name ?? Path.GetFileName(file).Replace("%", "");
        var uniqueName = FileHelper.GenerateUniqueFileName(localName, name => Files.Any(f => f.Model.Name == name));

        var item = CreateItem(uniqueName);
        PreviewAdd(item, file);

        OnChanged(new CustomChange(
            () =>
            {
                PreviewRemove(item);
                HasPendingChanges = IsChanged();
            },
            () =>
            {
                PreviewAdd(item, file);
                HasPendingChanges = IsChanged();
            }));

        return item;
    }

    private void ValidateFileExtensionAndSize(string fileName)
    {
        var fileExtensions = Quality.FileExtensions[_name];
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!fileExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                string.Format(
                    Resources.InvalidFileExtension,
                    fileName,
                    extension,
                    string.Join(", ", fileExtensions)));
        }

        var maximumSize = Quality.FileSizeMb[_name];

        if (new FileInfo(fileName).Length > maximumSize * 1024 * 1024)
        {
            throw new InvalidOperationException(
                string.Format(
                    Resources.InvalidFileSize,
                    fileName,
                    maximumSize));
        }
    }

    private void PreviewAdd(MediaItemViewModel item, string path, int index = -1)
    {
        if (_removed.Contains(item))
        {
            _removed.Remove(item);
        }
        else
        {
            FileStream fileStream;

            try
            {
                fileStream = File.OpenRead(path);
            }
            catch (Exception exc)
            {
                _logger.LogWarning(exc, "PreviewAdd error: {error}", exc.Message);
                OnError(exc);
                return;
            }

            _added.Add(item);
            _streams[item] = Tuple.Create(path, fileStream);

            if (_removedStreams.ContainsKey(item))
            {
                _removedStreams.Remove(item);
            }
        }

        if (index == -1)
        {
            Files.Add(item);
        }
        else
        {
            Files.Insert(index, item);
        }

        OnPropertyChanged(nameof(Files));

        HasPendingChanges = IsChanged();
    }

    internal long GetLength(string link) =>
        _document.Lock.WithLock(
            () =>
            {
                var collection = _document.GetInternalCollection(_name);
                return collection.GetFileLength(link);
            });

    // TODO: switch from IMedia to MediaInfo struct
    internal IMedia Wrap(string link)
    {
        // TODO: not a very effective way of handling pending renamed files, but other ways are more complex
        foreach (var item in _renamed)
        {
            if (item.Item2 == link)
            {
                link = item.Item1;
                break;
            }    
        }

        var pendingStream = _streams.FirstOrDefault(n => n.Key.Model.Name == link);

        if (pendingStream.Key != null)
        {
            return new Media(pendingStream.Value.Item1, pendingStream.Value.Item2.Length);
        }

        return _document.Lock.WithLock(
            () =>
            {
                var collection = _document.GetInternalCollection(_name); // This value cannot be cached as internal collection link can eventually change 
                
                return PlatformManager.Instance.PrepareMedia(
                    new Media(() => collection.GetFile(link), () => collection.GetFileLength(link), _internalId + link),
                    collection.Name);
            });
    }

    /// <summary>
    /// Tries to get global file path for a media file.
    /// </summary>
    /// <param name="mediaItem">Media file.</param>
    /// <returns>Global file path for media file or null.</returns>
    internal string? TryGetFilePath(MediaItemViewModel mediaItem)
    {
        var pendingStream = _streams.FirstOrDefault(n => n.Key.Model.Name == mediaItem.Model.Name);

        if (pendingStream.Key != null)
        {
            return pendingStream.Value.Item1;
        }

        return null;
    }

    /// <summary>
    /// Tries to get stream for a media file.
    /// </summary>
    /// <param name="mediaItem">Media file.</param>
    internal Stream? TryGetStream(MediaItemViewModel mediaItem)
    {
        var collection = _document.GetInternalCollection(_name);
        var streamInfo = collection.GetFile(mediaItem.Model.Name);
        return streamInfo?.Stream;
    }

    internal StorageChanges GetChanges() => new()
    {
        Added = _added.Select(item => _streams[item].Item1).ToArray(),
        Removed = _removed.Select(item => item.Model.Name).ToArray(),
        Renamed = _renamed.ToDictionary(item => item.Item1, item => item.Item2)
    };

    internal void RestoreChanges(StorageChanges storageChanges)
    {
        foreach (var item in storageChanges.Added)
        {
            AddFile(item);
        }

        foreach (var item in storageChanges.Removed)
        {
            var mediaItem = Files.FirstOrDefault(f => f.Model.Name == item);

            if (mediaItem != null)
            {
                PreviewRemove(mediaItem);
            }
        }

        foreach (var item in storageChanges.Renamed)
        {
            _renamed.Add(Tuple.Create(item.Key, item.Value));
        }
    }
}
