using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

public sealed class TagsViewModel : ItemsViewModel<string>
{
    internal PackageViewModel Owner { get; private set; }

    public override QDocument OwnerDocument => Owner.Document;

    public ICommand AddTag { get; private set; }

    public TagsViewModel(PackageViewModel owner, IEnumerable<string> collection)
        : base(collection)
    {
        Owner = owner;

        AddTag = new SimpleCommand(AddTag_Executed);

        UpdateCommands();
    }

    private void AddTag_Executed(object? arg)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }

        var index = CurrentPosition;

        if (string.IsNullOrWhiteSpace(this[index]))
        {
            this[index] = arg.ToString();
        }
        else
        {
            Add(arg.ToString());
        }
    }
}
