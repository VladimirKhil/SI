using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace SIQuester.Utilities
{
    /// <summary>
    /// Объект, который можно использовать для фильтрования коллекций
    /// </summary>
    public interface ICollectionFilter
    {
        void Filter(object sender, FilterEventArgs e);
    }
}
