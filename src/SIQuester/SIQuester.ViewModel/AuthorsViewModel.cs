using SIPackages;

namespace SIQuester.ViewModel;

public sealed class AuthorsViewModel : LinksViewModel
{
    public Authors Model { get; private set; }

    public AuthorsViewModel(Authors model, InfoViewModel owner)
        : base (model, owner)
    {
        Model = model;
    }

    protected override void LinkTo(int index, object arg) =>
        OwnerDocument.Document.SetAuthorLink(this, index, OwnerDocument.Document.Authors.IndexOf((AuthorInfo)arg));

    protected override bool CanRemove() => !(this == OwnerDocument?.Package?.Info?.Authors && Count == 1);
}
