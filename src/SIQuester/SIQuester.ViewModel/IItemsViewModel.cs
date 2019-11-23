using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    public interface IItemsViewModel: IList, INotifyPropertyChanged
    {
        int CurrentPosition { get; }

        void SetCurrentItem(object item);
    }
}
