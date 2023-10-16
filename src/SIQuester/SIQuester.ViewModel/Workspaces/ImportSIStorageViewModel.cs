using Microsoft.Extensions.Logging;
using SIQuester.ViewModel.Configuration;
using SIQuester.ViewModel.Properties;
using SIStorageService.ViewModel;
using System.ComponentModel;
using System.Net;

namespace SIQuester.ViewModel;

public sealed class ImportSIStorageViewModel : WorkspaceViewModel
{
    private static readonly HttpClient HttpClient = new() { DefaultRequestVersion = HttpVersion.Version20 };

    public StorageViewModel Storage { get; }

    public override string Header => Resources.SIStorage;

    private readonly StorageContextViewModel _storageContextViewModel;

    public bool IsProgress => Storage.IsLoading || Storage.IsLoadingPackages;

    private readonly AppOptions _appOptions;
    private readonly ILoggerFactory _loggerFactory;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public ImportSIStorageViewModel(
        StorageContextViewModel storageContextViewModel,
        StorageViewModel siStorage,
        AppOptions appOptions,
        ILoggerFactory loggerFactory)
    {
        _storageContextViewModel = storageContextViewModel;
        _appOptions = appOptions;
        _loggerFactory = loggerFactory;

        Storage = siStorage;

        Storage.Error += OnError;
        Storage.PropertyChanged += Storage_PropertyChanged;
    }

    internal Task OpenAsync() => Storage.OpenAsync(_cancellationTokenSource.Token);

    private void Storage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StorageViewModel.IsLoading) || e.PropertyName == nameof(StorageViewModel.IsLoadingPackages))
        {
            OnPropertyChanged(nameof(IsProgress));
        }
    }

    public async void Select()
    {
        if (Storage.CurrentPackage == null)
        {
            return;
        }

        async Task<QDocument> loader(Uri uri, CancellationToken cancellationToken)
        {
            var ms = new MemoryStream();

            using var response = await HttpClient.GetAsync(uri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync(cancellationToken));
            }

            await response.Content.CopyToAsync(ms, cancellationToken);

            ms.Position = 0;
            var doc = SIPackages.SIDocument.Load(ms);

            if (_appOptions.UpgradeNewPackages)
            {
                doc.Upgrade();
            }

            return new QDocument(doc, _storageContextViewModel, _loggerFactory) { FileName = doc.Package.Name };
        };

        var package = Storage.CurrentPackage;

        if (package == null)
        {
            return;
        }

        var uri = package.Model.ContentUri;

        if (uri == null)
        {
            return;
        }

        var loaderViewModel = new DocumentLoaderViewModel(package.Model.Name ?? "");
        OnNewItem(loaderViewModel);

        try
        {
            await loaderViewModel.LoadAsync(token => loader(uri, token));
        }
        catch (TaskCanceledException)
        {

        }
        catch (Exception ex)
        {
            OnError(ex);
        }
    }

    protected override void Dispose(bool disposing)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        base.Dispose(disposing);
    }
}
