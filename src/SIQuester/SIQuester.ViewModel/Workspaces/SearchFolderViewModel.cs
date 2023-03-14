using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Searches for some text value in files inside provided folder.
/// </summary>
/// <remarks>
/// The search is performed asynchronously when search value or target folder path changes.
/// </remarks>
public sealed class SearchFolderViewModel : WorkspaceViewModel
{
    public override string Header => Resources.FileSearch;

    /// <summary>
    /// Path of the folder to search.
    /// </summary>
    public string FolderPath
    {
        get => AppSettings.Default.SearchPath; // TODO: provide AppSettings via DI
        set
        {
            if (AppSettings.Default.SearchPath != value)
            {
                AppSettings.Default.SearchPath = value;
                OnPropertyChanged();
                StartSearch();
            }
        }
    }

    private CancellationTokenSource? _searchTokenSource = null;

    private readonly object _searchSync = new();

    private string _searchText = "";

    /// <summary>
    /// Text to search.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                StartSearch();
            }
        }
    }

    private bool _subfoldersSearch = true;

    /// <summary>
    /// Should the search be performed in subfolders (or only in the provided foler otherwise).
    /// </summary>
    public bool SubfoldersSearch
    {
        get => _subfoldersSearch;
        set
        {
            if (_subfoldersSearch != value)
            {
                _subfoldersSearch = value;
                OnPropertyChanged();
                StartSearch();
            }
        }
    }

    private byte _searchProgress = 0;

    /// <summary>
    /// Search progress in percentage from 0 to 100.
    /// </summary>
    public byte SearchProgress
    {
        get => _searchProgress;
        set
        {
            if (_searchProgress != value)
            {
                _searchProgress = value;
                OnPropertyChanged();
            }
        }
    }

    public SimpleCommand SelectFolderPath { get; private set; }

    public ICommand Open { get; private set; }

    public ObservableCollection<SearchResult> SearchResults { get; } = new();

    private readonly MainViewModel _main; // TODO: remove dependency

    public SearchFolderViewModel(MainViewModel main)
    {
        _main = main;

        SelectFolderPath = new SimpleCommand(SelectFolderPath_Executed);
        Open = new SimpleCommand(Open_Executed);
    }

    private async void StartSearch()
    {
        lock (_searchSync)
        {
            if (_searchTokenSource != null)
            {
                _searchTokenSource.Cancel(); // TODO: wait and Dispose
                _searchTokenSource = null;
            }
        }

        SearchResults.Clear();
        SearchProgress = 0;

        if (FolderPath.Length == 0 || !Directory.Exists(FolderPath) || _searchText.Length == 0)
        {
            return;
        }

        _searchTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Run(
                new Action(() =>
                {
                    var directoryInfo = new DirectoryInfo(FolderPath);
                    SearchInDirectory(directoryInfo, _searchText, _subfoldersSearch, _searchTokenSource.Token);
                }),
                _searchTokenSource.Token);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
        finally
        {
            SearchProgress = 0;
        }
    }

    private async void Open_Executed(object? arg)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }

        var result = (SearchResult)arg;

        var document = await _main.OpenFileAsync(result.FileName);

        if (document != null)
        {
            document.SearchText = _searchText;
            PlatformSpecific.PlatformManager.Instance.AddToRecentCategory(result.FileName);
        }
    }

    private void SelectFolderPath_Executed(object? arg)
    {
        var path = PlatformSpecific.PlatformManager.Instance.SelectSearchFolder();

        if (path != null)
        {
            FolderPath = path;
        }
    }

    private void SearchInDirectory(
        DirectoryInfo directoryInfo,
        string searchText,
        bool subfoldersSearch,
        CancellationToken token,
        int level = 0)
    {
        var files = directoryInfo.GetFiles("*.siq");
        var folders = directoryInfo.GetDirectories();
        var total = files.Length + (subfoldersSearch ? folders.Length : 0);
        var done = 0;
        SearchMatch? found = null;

        foreach (var file in files)
        {
            lock (_searchSync)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
            }

            found = null;

            try
            {
                using (var stream = File.OpenRead(file.FullName))
                {
                    using var doc = SIDocument.Load(stream);
                    var package = doc.Package;

                    if ((found = package.SearchFragment(searchText)) == null)
                    {
                        foreach (var round in package.Rounds)
                        {
                            if ((found = round.SearchFragment(searchText)) != null)
                            {
                                break;
                            }

                            foreach (var theme in round.Themes)
                            {
                                if ((found = theme.SearchFragment(searchText)) != null)
                                {
                                    break;
                                }

                                foreach (var quest in theme.Questions)
                                {
                                    if ((found = quest.SearchFragment(searchText)) != null)
                                    {
                                        break;
                                    }
                                }

                                if (found != null)
                                {
                                    break;
                                }
                            }

                            if (found != null)
                            {
                                break;
                            }
                        }
                    }
                }

                if (found != null)
                {
                    var fileNameLocal = file.FullName;
                    var foundLocal = found;

                    UI.Execute(
                        () =>
                        {
                            lock (_searchSync)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    return;
                                }

                                SearchResults.Add(new SearchResult
                                {
                                    FileName = fileNameLocal,
                                    Fragment = foundLocal
                                });
                            }
                        },
                        exc => OnError(exc),
                        token);
                }
            }
            catch
            {
                // TODO: log
            }
            finally
            {
                done++;

                if (level == 0)
                {
                    lock (_searchSync)
                    {
                        if (!token.IsCancellationRequested)
                        {
                            SearchProgress = (byte)(100 * done / total);
                        }
                    }
                }
            }
        }

        if (subfoldersSearch)
        {
            foreach (var dir in folders)
            {
                SearchInDirectory(dir, searchText, subfoldersSearch, token, level + 1);

                done++;

                if (level == 0)
                {
                    lock (_searchSync)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        SearchProgress = (byte)(100 * done / total);
                    }
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_searchTokenSource != null)
        {
            _searchTokenSource.Cancel();
            _searchTokenSource = null; // TODO: wait and dispose
        }

        base.Dispose(disposing);
    }
}
