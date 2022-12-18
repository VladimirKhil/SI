using System.Windows.Data;

namespace SIQuester.Utilities;

public sealed class QuestionTypeFilter : ICollectionFilter
{
    public void Filter(object sender, FilterEventArgs e)
    {
        e.Accepted = ((KeyValuePair<string, string>)e.Item).Key.Length > 0;
    }
}
