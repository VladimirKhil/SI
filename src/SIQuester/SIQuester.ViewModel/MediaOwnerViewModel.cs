using SIPackages.Core;
using SIQuester.ViewModel.Contracts;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents an media owner view model.
/// </summary>
/// <remarks>
/// Supports asynchronous media load.
/// </remarks>
/// <inheritdoc cref="ModelViewBase" />
public abstract class MediaOwnerViewModel : ModelViewBase, IMediaOwner
{
    private IMedia? _mediaSource = null;

    private Task<IMedia?>? _mediaLoading = null;

    private readonly object _mediaLoadingLock = new();

    private bool _mediaLoaded = false;

    /// <summary>
    /// Media source object.
    /// </summary>
    public IMedia? MediaSource
    {
        get
        {
            if (_mediaSource == null && !_mediaLoaded && _mediaLoading == null)
            {
                LoadMediaNoWait();
            }

            return _mediaSource;
        }
        set
        {
            if (_mediaSource != value)
            {
                _mediaSource = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _errorMessage;

    /// <summary>
    /// View model current error message (or null if there is no error).
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { if (_errorMessage != value) { _errorMessage = value; OnPropertyChanged(); } }
    }

    protected abstract IMedia GetMedia();

    protected abstract void OnError(Exception exc);

    private IMedia? LoadMedia()
    {
        try
        {
            var media = GetMedia();
            MediaSource = media;
            ErrorMessage = DetectErrorMessage(media);
            return _mediaSource;
        }
        finally
        {
            lock (_mediaLoadingLock)
            {
                _mediaLoaded = true;
                _mediaLoading = null;
            }
        }
    }

    protected virtual string? DetectErrorMessage(IMedia media) => null;

    public async ValueTask<IMedia?> LoadMediaAsync(CancellationToken cancellationToken = default)
    {
        if (_mediaLoaded)
        {
            return _mediaSource;
        }

        lock (_mediaLoadingLock)
        {
            if (_mediaLoaded)
            {
                return _mediaSource;
            }

            _mediaLoading ??= Task.Run(LoadMedia, cancellationToken);
        }

        return await _mediaLoading;
    }

    private async void LoadMediaNoWait()
    {
        try
        {
            await LoadMediaAsync();
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }
}
