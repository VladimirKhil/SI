using Microsoft.Extensions.Logging;
using SIQuester.Model;
using SIStorage.Service.Contract;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIQuester.ViewModel;

public sealed class StorageContextViewModel : INotifyPropertyChanged
{
    private readonly ISIStorageServiceClient _siStorageServiceClient;
    private readonly AppSettings _appSettings;
    private readonly ILogger<StorageContextViewModel> _logger;

    private string[] _publishers = Array.Empty<string>();

    public string[] Publishers
    {
        get => _publishers;
        set
        {
            _publishers = value;
            OnPropertyChanged();
        }
    }

    private string[] _authors = Array.Empty<string>();

    public string[] Authors
    {
        get => _authors;
        set
        {
            _authors = value;
            OnPropertyChanged();
        }
    }

    private string[] _tags = Array.Empty<string>();

    public string[] Tags
    {
        get => _tags;
        set
        {
            _tags = value;
            OnPropertyChanged();
        }
    }

    public string[] Languages { get; } = new string[] { "ru-RU", "en-US", "sr-RS" };

    public StorageContextViewModel(
        ISIStorageServiceClient siStorageService,
        AppSettings appSettings,
        ILogger<StorageContextViewModel> logger)
    {
        _siStorageServiceClient = siStorageService;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async void Load(CancellationToken cancellationToken = default)
    {
        try
        {
            var languages = await _siStorageServiceClient.Facets.GetLanguagesAsync(cancellationToken);
            var languageId = languages.FirstOrDefault(l => l.Code == _appSettings.Language)?.Id;

            Publishers = (await _siStorageServiceClient.Facets.GetPublishersAsync(languageId, cancellationToken))
                .Select(publisher => publisher.Name)
                .OrderBy(n => n)
                .ToArray();

            Tags = (await _siStorageServiceClient.Facets.GetTagsAsync(languageId, cancellationToken))
                .Select(tag => tag.Name)
                .OrderBy(n => n)
                .ToArray();
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Storage context load error: {error}", exc.Message);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
