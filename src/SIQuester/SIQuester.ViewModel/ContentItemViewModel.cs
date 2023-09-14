using SIPackages;
using SIPackages.Core;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents content item view model.
/// </summary>
/// <inheritdoc cref="MediaOwnerViewModel" />
public sealed class ContentItemViewModel : MediaOwnerViewModel
{
    /// <summary>
    /// Original model wrapped by this view model.
    /// </summary>
    public ContentItem Model { get; }

    /// <summary>
    /// View model that contains current view model.
    /// </summary>
    public ContentItemsViewModel? Owner { get; set; }

    public override string Type => CollectionNames.TryGetCollectionName(Model.Type) ?? Model.Type;

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

    public ContentItemViewModel(ContentItem model) => Model = model;

    protected override IMedia GetMedia()
    {
        if (Owner == null)
        {
            throw new InvalidOperationException("OwnerScenario is undefined");
        }

        if (Owner.OwnerDocument == null)
        {
            throw new InvalidOperationException("OwnerDocument is undefined");
        }

        return Owner.OwnerDocument.Wrap(Model);
    }

    protected override void OnError(Exception exc) => Owner?.OwnerDocument?.OnError(exc);
}
