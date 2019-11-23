using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIQuester.Utilities
{
    internal interface IDataContextOwner
    {
        object DataContext { get; set; }
    }
}
