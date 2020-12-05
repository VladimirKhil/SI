using SIPackages;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    public interface IItemViewModel
    {
        bool IsSelected { get; set; }
        bool IsExpanded { get; set; }

        InfoOwner GetModel();

        IItemViewModel Owner { get; }
        InfoViewModel Info { get; }

        ICommand Add { get; }
        ICommand Remove { get; }
    }
}
