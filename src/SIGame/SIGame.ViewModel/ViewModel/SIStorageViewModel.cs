using SIGame.ViewModel.Contracts;
using SIGame.ViewModel.PackageSources;
using SIGame.ViewModel.Properties;
using SIStorageService.ViewModel;
using Utils.Commands;

namespace SIGame.ViewModel;

public sealed class SIStorageViewModel : ViewModel<StorageViewModel>, INavigationNode
{
    private bool _isInitialized = false;

    public SimpleCommand LoadStorePackage { get; }

    private readonly IPackageSelector _packageSelector;

    public bool IsProgress => Model.IsLoading || Model.IsLoadingPackages || IsLoading;

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(nameof(IsProgress)); } }
    }

    public SIStorageViewModel(StorageViewModel siStorage, UserSettings userSettings, IPackageSelector packageSelector)
        : base(siStorage)
    {
        _packageSelector = packageSelector;

        _model.DefaultRestriction =  userSettings.Restriction;
        _model.DefaultPublisher = userSettings.Publisher;
        _model.DefaultTag = userSettings.Tag;

        LoadStorePackage = new SimpleCommand(LoadStorePackage_Executed) { CanBeExecuted = false };

        Model.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(StorageViewModel.CurrentRestriction):
                    userSettings.Restriction = Model.CurrentRestriction?.Value;
                    break;

                case nameof(StorageViewModel.CurrentPublisher):
                    userSettings.Publisher = Model.CurrentPublisher?.Name;
                    break;

                case nameof(StorageViewModel.CurrentTag):
                    userSettings.Tag = Model.CurrentTag?.Name;
                    break;

                case nameof(StorageViewModel.CurrentPackage):
                    LoadStorePackage.CanBeExecuted = Model.CurrentPackage != null;
                    break;

                case nameof(StorageViewModel.IsLoading):
                    OnPropertyChanged(nameof(IsProgress));
                    break;

                case nameof(StorageViewModel.IsLoadingPackages):
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
    }

    private void LoadStorePackage_Executed(object? arg)
    {
        try
        {
            IsLoading = true;

            var packageInfo = Model.CurrentPackage?.Model;

            if (packageInfo == null || packageInfo.ContentUri == null)
            {
                return;
            }

            var packageSource = new SIStoragePackageSource(packageInfo.ContentUri, 0, packageInfo.Name ?? "", packageInfo.Id.ToString());
            _packageSelector.SelectPackageSource(packageSource);
            
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

    public event Action? Close;

    private void OnClose() => Close?.Invoke();

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
