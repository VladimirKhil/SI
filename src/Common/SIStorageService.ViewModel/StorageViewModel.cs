using SIStorage.Service.Contract;
using SIStorage.Service.Contract.Models;
using SIStorage.Service.Contract.Requests;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIStorageService.ViewModel;

/// <summary>
/// Defines a SIStorage view model.
/// </summary>
public sealed class StorageViewModel : INotifyPropertyChanged
{
    private static readonly Tag AllTags = new(-2, "");
    private static readonly Publisher AllPublishers = new(-2, "");
    private static readonly Restriction AllRestrictions = new(-2, "", "");

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

    public event Action<Exception, string?>? Error;

    private PackageViewModel[] _packages = Array.Empty<PackageViewModel>();

    public PackageViewModel[] Packages
    {
        get => _packages;
        set
        {
            _packages = value;
            OnPropertyChanged();
        }
    }

    private PackageViewModel? _currentPackage;

    public PackageViewModel? CurrentPackage
    {
        get => _currentPackage;
        set { if (_currentPackage != value) { _currentPackage = value; OnPropertyChanged(); } }
    }

    private Publisher? _currentPublisher;

    public Publisher? CurrentPublisher
    {
        get => _currentPublisher;
        set
        {
            if (_currentPublisher != value)
            {
                _currentPublisher = value;
                OnPropertyChanged();
                LoadPackages();
            }
        }
    }

    private Publisher[] _publishers = Array.Empty<Publisher>();

    public Publisher[] Publishers
    {
        get => _publishers;
        set
        {
            _publishers = value;
            OnPropertyChanged();
        }
    }

    private Restriction? _currentRestriction;

    public Restriction? CurrentRestriction
    {
        get => _currentRestriction;
        set
        {
            if (_currentRestriction != value)
            {
                _currentRestriction = value;
                OnPropertyChanged();
                LoadPackages();
            }
        }
    }

    private Restriction[] _restrictions = Array.Empty<Restriction>();

    public Restriction[] Restrictions
    {
        get => _restrictions;
        set
        {
            _restrictions = value;
            OnPropertyChanged();
        }
    }

    private Author[] _authors = Array.Empty<Author>();

    public Author[] Authors
    {
        get => _authors;
        set
        {
            _authors = value;
            OnPropertyChanged();
        }
    }

    public PackageSortMode[] SortModes { get; } = new PackageSortMode[] { PackageSortMode.Name, PackageSortMode.CreatedDate };

    public PackageSortDirection[] SortDirections { get; } = new[] 
    {
        PackageSortDirection.Ascending,
        PackageSortDirection.Descending
    };

    private Tag? _currentTag;

    public Tag? CurrentTag
    {
        get => _currentTag;
        set
        {
            if (_currentTag != value)
            {
                _currentTag = value;
                OnPropertyChanged();
                LoadPackages();
            }
        }
    }

    private Tag[] _tags = Array.Empty<Tag>();

    public Tag[] Tags
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
                LoadPackages();
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
                LoadPackages();
            }
        }
    }

    private PackageSortDirection _currentSortDirection = PackageSortDirection.Ascending;

    public PackageSortDirection CurrentSortDirection
    {
        get => _currentSortDirection;
        set
        {
            if (_currentSortDirection != value)
            {
                _currentSortDirection = value;
                OnPropertyChanged();
                LoadPackages();
            }
        }
    }

    public string? DefaultPublisher { get; set; }

    public string? DefaultTag { get; set; }

    public string? DefaultRestriction { get; set; }

    public StorageViewModel() => throw new NotImplementedException();

    public StorageViewModel(ISIStorageServiceClient sIStorageServiceClient) =>
        _siStorageServiceClient = sIStorageServiceClient;

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;

        try
        {
            await Task.WhenAll(
                LoadPublishersAsync(cancellationToken),
                LoadTagsAsync(cancellationToken),
                LoadRestrictionsAsync(cancellationToken),
                LoadAuthorsAsync(cancellationToken));
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
        finally
        {
            IsLoading = false;
        }

        await LoadPackagesAsync(cancellationToken);
    }

    private async void LoadPackages(CancellationToken cancellationToken = default) => await LoadPackagesAsync(cancellationToken);

    private async Task LoadPackagesAsync(CancellationToken cancellationToken = default)
    {
        IsLoadingPackages = true;

        try
        {
            Packages = Array.Empty<PackageViewModel>();

            var tagId = _currentTag == AllTags ? null : _currentTag?.Id;
            var publisherId = _currentPublisher == AllPublishers ? null : _currentPublisher?.Id;
            var restrictionId = _currentRestriction == AllRestrictions ? null : _currentRestriction?.Id;

            var packages = await _siStorageServiceClient.Packages.GetPackagesAsync(
                new PackageFilters
                {
                    TagIds = tagId.HasValue ? new[] { tagId.Value } : null,
                    PublisherId = publisherId,
                    RestrictionIds = restrictionId.HasValue ? new[] { restrictionId.Value } : null,
                    SearchText = _filter
                },
                new PackageSelectionParameters
                {
                    SortMode = _currentSortMode,
                    SortDirection = _currentSortDirection,
                    Count = int.MaxValue
                },
                cancellationToken: cancellationToken);

            Packages = packages.Packages.Select(p => new PackageViewModel(p, _tags, _publishers, _restrictions, _authors)).ToArray();
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

    private void OnError(Exception exc, string? message = null) => Error?.Invoke(exc, message);

    private async Task LoadPublishersAsync(CancellationToken cancellationToken = default)
    {
        var publishers = (await _siStorageServiceClient.Facets.GetPublishersAsync(null, cancellationToken)).OrderBy(p => p.Name);
        Publishers = new[] { AllPublishers, new Publisher(-1, "") }.Concat(publishers).ToArray();

        if (Publishers.Length > 0)
        {
            _currentPublisher = DefaultPublisher != null
                ? Publishers.FirstOrDefault(p => p.Name == DefaultPublisher) ?? Publishers[0]
                : Publishers[0];

            OnPropertyChanged(nameof(CurrentPublisher));
        }
    }

    private async Task LoadTagsAsync(CancellationToken cancellationToken = default)
    {
        var tags = (await _siStorageServiceClient.Facets.GetTagsAsync(null, cancellationToken)).OrderBy(t => t.Name);
        Tags = new[] { AllTags, new Tag( -1, "") }.Concat(tags).ToArray();

        if (Tags.Length > 0)
        {
            _currentTag = DefaultTag != null ? Tags.FirstOrDefault(p => p.Name == DefaultTag) ?? Tags[0] : Tags[0];
            OnPropertyChanged(nameof(CurrentTag));
        }
    }

    private async Task LoadRestrictionsAsync(CancellationToken cancellationToken = default)
    {
        var restrictions = (await _siStorageServiceClient.Facets.GetRestrictionsAsync(cancellationToken)).OrderBy(r => r.Value);
        Restrictions = new[] { AllRestrictions, new Restriction(-1, "", "") }.Concat(restrictions).ToArray();

        if (Restrictions.Length > 0)
        {
            _currentRestriction = DefaultRestriction != null
                ? Restrictions.FirstOrDefault(p => p.Value == DefaultRestriction) ?? Restrictions[0]
                : Restrictions[0];

            OnPropertyChanged(nameof(CurrentRestriction));
        }
    }

    private async Task LoadAuthorsAsync(CancellationToken cancellationToken = default)
    {
        Authors = await _siStorageServiceClient.Facets.GetAuthorsAsync(null, cancellationToken);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
