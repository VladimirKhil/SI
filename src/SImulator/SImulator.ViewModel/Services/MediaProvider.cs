using SImulator.ViewModel.Contracts;
using SIPackages;
using SIPackages.Core;

namespace SImulator.ViewModel.Services;

internal sealed class MediaProvider : IMediaProvider
{
    private readonly SIDocument _document;

    public MediaProvider(SIDocument document) => _document = document;

    public MediaInfo? TryGetMedia(ContentItem contentItem) => _document.TryGetMedia(contentItem);
}
