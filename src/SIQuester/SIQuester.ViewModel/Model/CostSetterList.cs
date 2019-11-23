using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using SIQuester.ViewModel;
using System.Collections.ObjectModel;

namespace SIQuester.Model
{
    public sealed class CostSetterList: ObservableCollection<CostSetter>
    {
        public ICommand AddItem { get; set; }
        public ICommand DeleteItem { get; set; }

        public CostSetterList()
        {
            AddItem = new SimpleCommand(AddItem_Executed);
            DeleteItem = new SimpleCommand(DeleteItem_Executed);
        }

        private void AddItem_Executed(object arg)
        {
            Add(new CostSetter());
        }

        private void DeleteItem_Executed(object arg)
        {
            Remove((CostSetter)arg);
        }
    }
}
