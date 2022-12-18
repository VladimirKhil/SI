using SIQuester.ViewModel;
using System.Collections.ObjectModel;

namespace SIQuester.Model;

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
