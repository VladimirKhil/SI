using SIStorageService.Client;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SIQuester.ViewModel;

public sealed class StorageContextViewModel : INotifyPropertyChanged
{
    private readonly ISIStorageServiceClient _siStorageServiceClient;

    private string[] _publishers;

    public string[] Publishers
    {
        get => _publishers;
        set
        {
            _publishers = value;
            OnPropertyChanged();
        }
    }

    private string[] _authors;

    public string[] Authors
    {
        get => _authors;
        set
        {
            _authors = value;
            OnPropertyChanged();
        }
    }

    private string[] _tags;

    public string[] Tags
    {
        get => _tags;
        set
        {
            _tags = value;
            OnPropertyChanged();
        }
    }

    public string[] Languages { get; } = new string[] { "ru-RU", "en-US" };

    public StorageContextViewModel(ISIStorageServiceClient siStorageService)
    {
        _siStorageServiceClient = siStorageService;
    }

    public async void Load()
    {
        try
        {
            Publishers = (await _siStorageServiceClient.GetPublishersAsync()).Select(named => named.Name).ToArray();
            Tags = (await _siStorageServiceClient.GetTagsAsync()).Select(named => named.Name).ToArray();
        }
        catch (Exception exc)
        {
            Trace.TraceWarning(exc.ToString());
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
