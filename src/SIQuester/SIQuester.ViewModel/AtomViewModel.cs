using SIPackages;
using SIPackages.Core;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents question scenario atom view model.
/// </summary>
/// <inheritdoc cref="MediaOwnerViewModel" />
public sealed class AtomViewModel : MediaOwnerViewModel
{
    /// <summary>
    /// Original model wrapped by this view model.
    /// </summary>
    public Atom Model { get; }

    /// <summary>
    /// Media item type.
    /// </summary>
    public override string Type => Model.Type switch
    {
        AtomTypes.Image => CollectionNames.ImagesStorageName,
        AtomTypes.Audio => CollectionNames.AudioStorageName,
        AtomTypes.AudioNew => CollectionNames.AudioStorageName,
        AtomTypes.Video => CollectionNames.VideoStorageName,
        AtomTypes.Html => CollectionNames.HtmlStorageName,
        _ => Model.Type,
    };

    /// <summary>
    /// Scenario view model that contains current view model.
    /// </summary>
    public ScenarioViewModel? OwnerScenario { get; set; }

    private bool _isExpanded = true;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
    }

    public AtomViewModel(Atom model) => Model = model;

    protected override IMedia GetMedia()
    {
        if (OwnerScenario == null)
        {
            throw new InvalidOperationException("OwnerScenario is undefined");
        }

        if (OwnerScenario.OwnerDocument == null)
        {
            throw new InvalidOperationException("OwnerDocument is undefined");
        }

        return OwnerScenario.OwnerDocument.Wrap(Model);
    }

    protected override void OnError(Exception exc) => OwnerScenario?.OwnerDocument?.OnError(exc);
}
