using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIQuester.Utilities
{
    public sealed class QuestionTypeFilter: ICollectionFilter
    {
        public void Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            e.Accepted = ((KeyValuePair<string, string>)e.Item).Key.Length > 0;
        }
    }
}
