using System.Windows.Data;

namespace SIQuester.Utilities;

/// <summary>
/// Объект, который можно использовать для фильтрации коллекций
/// </summary>
public interface ICollectionFilter
{
    void Filter(object sender, FilterEventArgs e);
}
