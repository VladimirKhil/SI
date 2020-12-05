using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Core;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public sealed class SearchFolderViewModel: WorkspaceViewModel
    {
        public override string Header => "Поиск файлов";

        public string FolderPath
        {
            get { return AppSettings.Default.SearchPath; }
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

        private CancellationTokenSource _searchLink = null;
        private readonly object _searchSync = new object();

        private async void StartSearch()
        {
            lock (_searchSync)
            {
                if (_searchLink != null)
                {
                    _searchLink.Cancel();
                }
            }

            SearchResults.Clear();
            SearchProgress = 0;

            if (FolderPath.Length == 0 || !Directory.Exists(FolderPath) || _searchText.Length == 0)
                return;

            _searchLink = new CancellationTokenSource();
            var task = Task.Run(new Action(() =>
            {
                var directoryInfo = new DirectoryInfo(FolderPath);
                SearchInDirectory(directoryInfo, _searchText, _subfoldersSearch, _searchLink.Token);
            }), _searchLink.Token);

            try
            {
                await task;
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

        private string _searchText = "";

        public string SearchText
        {
            get { return _searchText; }
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

        public bool SubfoldersSearch
        {
            get { return _subfoldersSearch; }
            set
            {
                if (_subfoldersSearch != value)
                {
                    _subfoldersSearch = value;
                    StartSearch();
                }
            }
        }

        private int _searchProgress = 0;

        public int SearchProgress
        {
            get { return _searchProgress; }
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

        public ObservableCollection<SearchResult> SearchResults { get; } = new ObservableCollection<SearchResult>();

        private readonly MainViewModel _main = null; // TODO: убрать такую зависимость

        public SearchFolderViewModel(MainViewModel main)
        {
            _main = main;

            SelectFolderPath = new SimpleCommand(SelectFolderPath_Executed);
            Open = new SimpleCommand(Open_Executed);
        }

        private void Open_Executed(object arg)
        {
            var result = (SearchResult)arg;
            _main.OpenFile(result.FileName, _searchText, onSuccess: () =>
            {
                PlatformSpecific.PlatformManager.Instance.AddToRecentCategory(result.FileName);
            });
        }

        private void SelectFolderPath_Executed(object arg)
        {
            var path = PlatformSpecific.PlatformManager.Instance.SelectSearchFolder();
            if (path != null)
                FolderPath = path;
        }

        private void SearchInDirectory(DirectoryInfo directoryInfo, string searchText, bool subfoldersSearch, CancellationToken token, int level = 0)
        {
            var files = directoryInfo.GetFiles("*.siq");
            var folders = directoryInfo.GetDirectories();
            var total = files.Length + (subfoldersSearch ? folders.Length : 0);
            var done = 0;
            SearchMatch found = null;

            foreach (var file in files)
            {
                lock (_searchSync)
                {
                    if (token.IsCancellationRequested)
                        return;
                }

                found = null;
                try
                {
                    using (var stream = File.OpenRead(file.FullName))
                    {
                        using (var doc = SIDocument.Load(stream))
                        {
                            var package = doc.Package;
                            if ((found = package.SearchFragment(searchText)) == null)
                            {
                                foreach (var round in package.Rounds)
                                {
                                    if ((found = round.SearchFragment(searchText)) != null)
                                        break;

                                    foreach (var theme in round.Themes)
                                    {
                                        if ((found = theme.SearchFragment(searchText)) != null)
                                            break;

                                        foreach (var quest in theme.Questions)
                                        {
                                            if ((found = quest.SearchFragment(searchText)) != null)
                                                break;
                                        }

                                        if (found != null)
                                            break;
                                    }

                                    if (found != null)
                                        break;
                                }
                            }
                            else
                            {
                            }
                        }
                    }

                    if (found != null)
                    {
                        var fileNameLocal = file.FullName;
                        var foundLocal = found;
                        Task.Factory.StartNew(() =>
                        {
                            lock (_searchSync)
                            {
                                if (token.IsCancellationRequested)
                                    return;

                                SearchResults.Add(new SearchResult
                                {
                                    FileName = fileNameLocal,
                                    Fragment = foundLocal
                                });
                            }
                        }, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
                    }
                }
                catch
                {
                }
                finally
                {
                    done++;
                    if (level == 0)
                    {
                        lock (_searchSync)
                        {
                            if (!token.IsCancellationRequested)
                                SearchProgress = 100 * done / total;
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
                                return;

                            SearchProgress = 100 * done / total;
                        }
                    }
                }
            }
        }
    }
}
