using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using SIQuester.ViewModel;

namespace SIQuester.Model
{
    public sealed class SearchResults
    {
        public string Query { get; set; }
        public Collection<IItemViewModel> Results { get; set; }
        public int Index { get; set; }

        public SearchResults()
        {
            Results = new Collection<IItemViewModel>();
            Index = 0;
        }
    }
}
