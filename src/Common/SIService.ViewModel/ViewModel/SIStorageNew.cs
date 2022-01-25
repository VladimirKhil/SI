using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Services.SI.ViewModel
{
    public sealed class SIStorageNew : INotifyPropertyChanged
    {
        private SIStorageServiceClient _siService;

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private bool _isLoadingPackages;

        public bool IsLoadingPackages
        {
            get => _isLoadingPackages;
            set { _isLoadingPackages = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<Exception> Error;

        private PackageInfo[] _packages;

        public PackageInfo[] Packages
        {
            get
            {
                return _packages;
            }
            set
            {
                _packages = value;
                OnPropertyChanged();
            }
        }

        private PackageInfo[] _filteredPackages;

        public PackageInfo[] FilteredPackages
        {
            get
            {
                return _filteredPackages;
            }
            set
            {
                _filteredPackages = value;
                OnPropertyChanged();
            }
        }

        private PackageInfo _currentPackage;

        public PackageInfo CurrentPackage
        {
            get { return _currentPackage; }
            set { if (_currentPackage != value) { _currentPackage = value; OnPropertyChanged(); } }
        }

        private NamedObject _currentPublisher;

        public NamedObject CurrentPublisher
        {
            get { return _currentPublisher; }
            set
            {
                if (_currentPublisher != value)
                {
                    _currentPublisher = value;
                    OnPropertyChanged();
                    LoadPackagesAsync();
                }
            }
        }

        public Task<Uri> LoadSelectedPackageUriAsync()
        {
            if (_siService == null || CurrentPackage == null)
            {
                return Task.FromResult<Uri>(null);
            }

            return _siService.GetPackageByIDAsync(CurrentPackage.ID);
        }

        private NamedObject[] _publishers;

        public NamedObject[] Publishers
        {
            get
            {
                return _publishers;
            }
            set
            {
                _publishers = value;
                OnPropertyChanged();
            }
        }

        private string _currentRestriction;

        public string CurrentRestriction
        {
            get { return _currentRestriction; }
            set
            {
                if (_currentRestriction != value)
                {
                    _currentRestriction = value;
                    OnPropertyChanged();
                    LoadPackagesAsync();
                }
            }
        }

        public string[] Restrictions { get; } = new string[] { "18+", "12+", " " };

        public PackageSortMode[] SortModes { get; } = new PackageSortMode[] { PackageSortMode.Name, PackageSortMode.PublishedDate };

        public bool[] SortDirections { get; } = new bool[] { true, false };

        private NamedObject _currentTag;

        public NamedObject CurrentTag
        {
            get { return _currentTag; }
            set
            {
                if (_currentTag != value)
                {
                    _currentTag = value;
                    OnPropertyChanged();
                    LoadPackagesAsync();
                }
            }
        }

        private NamedObject[] _tags;

        public NamedObject[] Tags
        {
            get
            {
                return _tags;
            }
            set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        private string _filter;

        public string Filter
        {
            get { return _filter; }
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    OnPropertyChanged();
                    FilterPackages();
                }
            }
        }

        private PackageSortMode _currentSortMode = PackageSortMode.Name;

        public PackageSortMode CurrentSortMode
        {
            get { return _currentSortMode; }
            set
            {
                if (_currentSortMode != value)
                {
                    _currentSortMode = value;
                    OnPropertyChanged();
                    LoadPackagesAsync();
                }
            }
        }

        private bool _currentSortDirection = true;

        public bool CurrentSortDirection
        {
            get { return _currentSortDirection; }
            set
            {
                if (_currentSortDirection != value)
                {
                    _currentSortDirection = value;
                    OnPropertyChanged();
                    LoadPackagesAsync();
                }
            }
        }

        public string DefaultPublisher { get; internal set; }
        public string DefaultTag { get; internal set; }

        public SIStorageNew()
        {
            
        }

        public void Open()
        {
            _siService = new SIStorageServiceClient();
            IsLoading = true;
            LoadPublishersAsync();
        }

        private async void LoadPackagesAsync()
        {
            if (_siService == null)
            {
                return;
            }

            IsLoadingPackages = true;
            try
            {
                Packages = null;
                FilteredPackages = null;

                var tagID = _currentTag == All ? null : _currentTag?.ID;
                var publisherId = _currentPublisher == All ? null : _currentPublisher?.ID;

                var packages = await _siService.GetPackagesAsync(tagId: tagID,
                    publisherId: publisherId,
                    restriction: _currentRestriction,
                    sortMode: _currentSortMode,
                    sortAscending: _currentSortDirection);

                Packages = packages;
                FilterPackages();
            }
            catch (Exception exc)
            {
                Error?.Invoke(exc);
            }
            finally
            {
                IsLoadingPackages = false;
            }
        }

        private static readonly NamedObject All = new NamedObject { ID = -2 };

        private async void LoadPublishersAsync()
        {
            try
            {
                var publishers = await _siService.GetPublishersAsync();

                Publishers = new[] { All, new NamedObject { ID = -1, Name = null } }.Concat(publishers).ToArray();
                if (_publishers.Length > 0)
                {
                    // Без асинхронной загрузки пакетов

                    if (DefaultPublisher != null)
                    {
                        _currentPublisher = _publishers.FirstOrDefault(p => p.Name == DefaultPublisher) ?? _publishers[0];
                    }
                    else
                    {
                        _currentPublisher = _publishers[0];
                    }

                    OnPropertyChanged(nameof(CurrentPublisher));
                }

                LoadTagsAsync();
            }
            catch (Exception exc)
            {
                Error?.Invoke(exc);
                IsLoading = false;
            }
        }

        private async void LoadTagsAsync()
        {
            try
            {
                var tags = await _siService.GetTagsAsync();

                Tags = new[] { All, new NamedObject { ID = -1, Name = null } }.Concat(tags).ToArray();
                if (_tags.Length > 0)
                {
                    // Без асинхронной загрузки пакетов
                    if (DefaultTag != null)
                    {
                        _currentTag = _tags.FirstOrDefault(p => p.Name == DefaultTag) ?? _tags[0];
                    }
                    else
                    {
                        _currentTag = _tags[0];
                    }

                    OnPropertyChanged(nameof(CurrentTag));
                }

                LoadPackagesAsync();
            }
            catch (Exception exc)
            {
                Error?.Invoke(exc);
            }
            
            IsLoading = false;
        }

        private void FilterPackages()
        {
            FilteredPackages = _filter == null ? _packages
                : _packages?
                    .Where(package => package.Description.ToLower()
                    .Contains(_filter.ToLower()))
                    .ToArray();

            CurrentPackage = _filteredPackages?.FirstOrDefault();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
