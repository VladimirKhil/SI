using System.Collections.Generic;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public sealed class TagsViewModel : ItemsViewModel<string>
    {
        internal PackageViewModel Owner { get; private set; }

        public override QDocument OwnerDocument
        {
            get
            {
                return Owner.Document;
            }
        }

        public ICommand AddTag { get; private set; }

        public TagsViewModel(PackageViewModel owner, IEnumerable<string> collection)
            : base(collection)
        {
            Owner = owner;

            AddTag = new SimpleCommand(AddTag_Executed);

            UpdateCommands();
        }

        private void AddTag_Executed(object arg)
        {
            var index = CurrentPosition;
            if (string.IsNullOrWhiteSpace(this[index]))
                this[index] = arg.ToString();
            else
                Add(arg.ToString());
        }
    }
}
