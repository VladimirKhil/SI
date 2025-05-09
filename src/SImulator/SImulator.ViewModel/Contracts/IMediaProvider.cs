using SIPackages.Core;
using SIPackages;

namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Allows to get media files.
/// </summary>
public interface IMediaProvider
{
    /// <summary>
    /// Gets media file for the given content item.
    /// </summary>
    /// <param name="contentItem">Content item.</param>
    MediaInfo? TryGetMedia(ContentItem contentItem);
}
