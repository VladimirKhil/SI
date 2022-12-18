using System.Collections;
using System.ComponentModel;

namespace SIQuester.ViewModel;

public interface IItemsViewModel : IList, INotifyPropertyChanged
{
    int CurrentPosition { get; }

    void SetCurrentItem(object item);
}
