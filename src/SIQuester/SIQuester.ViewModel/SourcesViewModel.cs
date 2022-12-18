using SIPackages;

namespace SIQuester.ViewModel;

public sealed class SourcesViewModel : LinksViewModel
{
    public Sources Model { get; private set; }

    public SourcesViewModel(Sources model, InfoViewModel owner)
        : base(model, owner) => Model = model;

    protected override void LinkTo(int index, object arg) =>
        OwnerDocument.Document.SetSourceLink(this, index, OwnerDocument.Document.Sources.IndexOf((SourceInfo)arg));
}
