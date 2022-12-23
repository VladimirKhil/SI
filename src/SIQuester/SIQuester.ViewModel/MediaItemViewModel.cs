using SIPackages.Core;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a sidebar media object view model.
/// </summary>
public sealed class MediaItemViewModel : MediaOwnerViewModel
{
    /// <summary>
    /// Media item type.
    /// </summary>
    public override string Type { get; }

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
}
