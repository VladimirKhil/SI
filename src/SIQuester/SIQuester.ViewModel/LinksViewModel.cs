using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

public abstract class LinksViewModel : ItemsViewModel<string>
{
    public InfoViewModel Owner { get; private set; }

    public ICommand LinkItem { get; private set; }

    /// <summary>
    /// Removes all items as one change (one undo restores them).
    /// </summary>
    public SimpleCommand ClearAll { get; private set; }

    public override QDocument? OwnerDocument
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
        ClearAll = new SimpleCommand(ClearAll_Executed) { CanBeExecuted = CanClear() };

        UpdateCommands();
    }

    private void ClearAll_Executed(object? arg)
    {
        if (Count == 0 || !CanClear())
        {
            return;
        }

        try
        {
            using var change = OwnerDocument?.OperationsManager.BeginComplexChange();

            while (Count > 0)
            {
                RemoveAt(Count - 1);
            }

            change?.Commit();
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowErrorMessage(exc.Message);
        }
    }

    private void LinkItem_Executed(object? arg)
    {
        if (arg == null)
        {
            return;
        }

        var index = CurrentPosition;
        LinkTo(index, arg);
    }

    /// <summary>
    /// Can all items be removed at once.
    /// </summary>
    /// <remarks>Called from the constructor.</remarks>
    protected virtual bool CanClear() => true;

    protected abstract void LinkTo(int index, object arg);

    public override string ToString() => string.Join(", ", this);
}
