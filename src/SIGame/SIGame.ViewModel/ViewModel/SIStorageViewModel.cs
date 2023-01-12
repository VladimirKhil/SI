using SIGame.ViewModel.PackageSources;
using SIGame.ViewModel.Properties;
using SIStorageService.ViewModel;
using Utils.Commands;

namespace SIGame.ViewModel;

public sealed class SIStorageViewModel : ViewModel<SIStorage>, INavigationNode
{
    public AsyncCommand LoadStorePackage { get; internal set; }

    internal event Action<PackageSource> AddPackage;

    public bool IsProgress => Model.IsLoading || Model.IsLoadingPackages || IsLoading;

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(nameof(IsProgress)); } }
    }

    public SIStorageViewModel(SIStorage siStorage, UserSettings userSettings)
        : base(siStorage)
    {
        _model.CurrentRestriction = userSettings.Restriction;

        _model.DefaultPublisher = userSettings.Publisher;
        _model.DefaultTag = userSettings.Tag;

        Model.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(SIStorage.CurrentRestriction):
                    userSettings.Restriction = Model.CurrentRestriction;
                    break;

                case nameof(SIStorage.CurrentPublisher):
                    userSettings.Publisher = Model.CurrentPublisher.Name;
                    break;

                case nameof(SIStorage.CurrentTag):
                    userSettings.Tag = Model.CurrentTag.Name;
                    break;

                case nameof(SIStorage.CurrentPackage):
                    LoadStorePackage.CanBeExecuted = Model.CurrentPackage != null;
                    break;

                case nameof(SIStorage.IsLoading):
                    OnPropertyChanged(nameof(IsProgress));
                    break;

                case nameof(SIStorage.IsLoadingPackages):
                    OnPropertyChanged(nameof(IsProgress));
                    break;
            }
        };

        Model.Error += (exc, message) =>
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                $"{message ?? Resources.SIStorageError}: {exc.Message}",
                PlatformSpecific.MessageType.Warning);
        };

        LoadStorePackage = new AsyncCommand(LoadStorePackage_Executed) { CanBeExecuted = false };
    }

    private async Task LoadStorePackage_Executed(object arg)
    {
        try
        {
            IsLoading = true;

            var packageInfo = Model.CurrentPackage;
            var link = await Model.LoadSelectedPackageUriAsync();

            var packageSource = new SIStoragePackageSource(link.Uri, packageInfo.ID, packageInfo.Description, packageInfo.Guid);

            AddPackage?.Invoke(packageSource);
            
            IsLoading = false;
        }
        catch (Exception exc)
        {
            IsLoading = false;
            PlatformSpecific.PlatformManager.Instance.ShowMessage($"{Resources.SIStorageCallError}: {exc.Message}", PlatformSpecific.MessageType.Warning);
        }
        finally
        {
            OnClose();
        }
    }

    public event Action Close;

    private void OnClose()
    {
        Close?.Invoke();
    }

    private bool _isInitialized = false;

    internal async Task InitAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await Model.OpenAsync();
        _isInitialized = true;
    }
}
