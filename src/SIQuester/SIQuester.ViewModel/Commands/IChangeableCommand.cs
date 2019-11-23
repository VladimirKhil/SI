using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIQuester.ViewModel
{
    public interface IChangeableCommand
    {
        void OnCanExecuteChanged();
    }
}
