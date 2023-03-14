using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

public abstract class LinksViewModel : ItemsViewModel<string>
{
    public InfoViewModel Owner { get; private set; }

    public ICommand LinkItem { get; private set; }

    public override QDocument OwnerDocument
    {
        get
        {
            var owner = Owner?.Owner;

            while (owner?.Owner != null)
            {
                owner = owner.Owner;
            }

            return (owner as PackageViewModel)?.Document;
        }
    }

    protected LinksViewModel(IEnumerable<string> model, InfoViewModel owner)
        : base(model)
    {
        Owner = owner;

        LinkItem = new SimpleCommand(LinkItem_Executed);

        UpdateCommands();
    }

    private void LinkItem_Executed(object? arg)
    {
        var index = CurrentPosition;
        LinkTo(index, arg);
    }

    protected abstract void LinkTo(int index, object arg);

    public override string ToString() => string.Join(", ", this);
}
