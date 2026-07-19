using System.Windows.Data;

namespace SIQuester.Utilities;

public sealed class QuestionTypeNameFilter : ICollectionFilter
{
    public void Filter(object sender, FilterEventArgs e)
    {
        var value = e.Item as string;
        e.Accepted = !string.IsNullOrEmpty(value);
    }
}
