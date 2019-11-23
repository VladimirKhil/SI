using SIPackages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
