using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Properties;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a sidebar media object view model.
/// </summary>
public sealed class MediaItemViewModel : MediaOwnerViewModel
{
    private static readonly Dictionary<string, int> RecommenedSizeMb = new()
    {
        [SIDocument.ImagesStorageName] = 1,
        [SIDocument.AudioStorageName] = 5,
        [SIDocument.VideoStorageName] = 10,
    };

    private static readonly Dictionary<string, string[]> RecommenedExtensions = new()
    {
        [SIDocument.ImagesStorageName] = new[] { ".jpg", ".jpeg", ".png" },
        [SIDocument.AudioStorageName] = new[] { ".mp3" },
        [SIDocument.VideoStorageName] = new[] { ".mp4" },
    };

    /// <summary>
    /// Media item type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Underlying media object.
    /// </summary>
    public Named Model { get; }

    private readonly Func<IMedia> _mediaGetter;

    public MediaItemViewModel(Named named, string type, Func<IMedia> mediaGetter)
    {
        Model = named;
        Type = type;
        _mediaGetter = mediaGetter;
    }

    protected override IMedia GetMedia() => _mediaGetter();

    protected override void OnError(Exception exc) => MainViewModel.ShowError(exc);

    protected override string? DetectErrorMessage(IMedia media)
    {
        var extension = Path.GetExtension(media.Uri).ToLowerInvariant();

        var sizeWarning = RecommenedSizeMb.TryGetValue(Type, out var recommendedMaxSize)
            && media.StreamLength > recommendedMaxSize * 1024 * 1024
                ? string.Format(Resources.MediaFileSizeExceedsRecommenedValue, recommendedMaxSize)
                : null;

        var extensionWarning = RecommenedExtensions.TryGetValue(Type, out var recommendedExtensions)
            && !recommendedExtensions.Contains(extension)
                ? string.Format(Resources.MediaFileExtensionIsNotRecommened, string.Join(',', recommendedExtensions))
                : null;

        return sizeWarning == null ? extensionWarning : (extensionWarning == null ? sizeWarning : $"{sizeWarning} {extensionWarning}");
    }
}
