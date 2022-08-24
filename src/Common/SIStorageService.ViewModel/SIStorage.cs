using SIStorageService.Client;
using SIStorageService.Client.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIStorageService.ViewModel
{
    /// <summary>
    /// Defines a SI Storage view model.
    /// </summary>
    public sealed class SIStorage : INotifyPropertyChanged
    {
        private readonly ISIStorageServiceClient _siStorageServiceClient;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        public event Action<Exception, string>? Error;

        private PackageInfo[]? _packages;

        public PackageInfo[]? Packages
        {
            get => _packages;
            set
            {
                _packages = value;
                OnPropertyChanged();
            }
        }

        private PackageInfo[]? _filteredPackages;

        public PackageInfo[]? FilteredPackages
        {
            get => _filteredPackages;
            set
            {
                _filteredPackages = value;
                OnPropertyChanged();
            }
        }

        private PackageInfo? _currentPackage;

        public PackageInfo? CurrentPackage
        {
            get => _currentPackage;
            set { if (_currentPackage != value) { _currentPackage = value; OnPropertyChanged(); } }
        }

        private NamedObject? _currentPublisher;

        public NamedObject? CurrentPublisher
        {
            get => _currentPublisher;
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

        public Task<PackageLink> LoadSelectedPackageUriAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentPackage == null)
            {
                throw new InvalidOperationException("CurrentPackage is undefined");
            }

            return _siStorageServiceClient.GetPackageByGuid2Async(CurrentPackage.Guid, cancellationToken);
        }

        private NamedObject[]? _publishers;

        public NamedObject[]? Publishers
        {
            get => _publishers;
            set
            {
                _publishers = value;
                OnPropertyChanged();
            }
        }

        private string? _currentRestriction;

        public string? CurrentRestriction
        {
            get => _currentRestriction;
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

        private NamedObject? _currentTag;

        public NamedObject? CurrentTag
        {
            get => _currentTag;
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

        private NamedObject[]? _tags;

        public NamedObject[]? Tags
        {
            get => _tags;
            set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        private string? _filter;

        public string? Filter
        {
            get => _filter;
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
            get => _currentSortMode;
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
            get => _currentSortDirection;
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

        public string? DefaultPublisher { get; set; }

        public string? DefaultTag { get; set; }

        public SIStorage() => throw new NotImplementedException();

        public SIStorage(ISIStorageServiceClient sIStorageServiceClient)
        {
            _siStorageServiceClient = sIStorageServiceClient;
        }

        public void Open()
        {
            IsLoading = true;
            LoadPublishersAsync();
        }

        private async void LoadPackagesAsync()
        {
            IsLoadingPackages = true;

            try
            {
                Packages = null;
                FilteredPackages = null;

                var tagID = _currentTag == All ? null : _currentTag?.ID;
                var publisherId = _currentPublisher == All ? null : _currentPublisher?.ID;

                var packages = await _siStorageServiceClient.GetPackagesAsync(tagId: tagID,
                    publisherId: publisherId,
                    restriction: _currentRestriction,
                    sortMode: _currentSortMode,
                    sortAscending: _currentSortDirection);

                Packages = packages;
                FilterPackages();
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
            finally
            {
                IsLoadingPackages = false;
            }
        }

        private void OnError(Exception exc, string message = null) => Error?.Invoke(exc, message);

        private static readonly NamedObject All = new() { ID = -2 };

        private async void LoadPublishersAsync()
        {
            try
            {
                var publishers = await _siStorageServiceClient.GetPublishersAsync();

                Publishers = new[] { All, new NamedObject { ID = -1, Name = null } }.Concat(publishers).ToArray();
                
                if (Publishers.Length > 0)
                {
                    // Без асинхронной загрузки пакетов

                    if (DefaultPublisher != null)
                    {
                        _currentPublisher = Publishers.FirstOrDefault(p => p.Name == DefaultPublisher) ?? Publishers[0];
                    }
                    else
                    {
                        _currentPublisher = Publishers[0];
                    }

                    OnPropertyChanged(nameof(CurrentPublisher));
                }

                LoadTagsAsync();
            }
            catch (Exception exc)
            {
                OnError(exc);
                IsLoading = false;
            }
        }

        private async void LoadTagsAsync()
        {
            try
            {
                var tags = await _siStorageServiceClient.GetTagsAsync();

                Tags = new[] { All, new NamedObject { ID = -1, Name = null } }.Concat(tags).ToArray();

                if (Tags.Length > 0)
                {
                    if (DefaultTag != null)
                    {
                        _currentTag = Tags.FirstOrDefault(p => p.Name == DefaultTag) ?? Tags[0];
                    }
                    else
                    {
                        _currentTag = Tags[0];
                    }

                    OnPropertyChanged(nameof(CurrentTag));
                }

                LoadPackagesAsync();
            }
            catch (Exception exc)
            {
                OnError(exc);
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

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
