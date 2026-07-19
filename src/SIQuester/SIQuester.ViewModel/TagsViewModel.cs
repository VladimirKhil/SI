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
        ArgumentNullException.ThrowIfNull(arg);
        var value = arg.ToString();

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var index = CurrentPosition;

        if (string.IsNullOrWhiteSpace(this[index]))
        {
            this[index] = value;
        }
        else
        {
            Add(value);
        }
    }
}
