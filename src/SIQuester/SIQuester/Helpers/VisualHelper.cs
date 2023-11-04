using System.Windows.Media;
using System.Windows;

namespace SIQuester.Helpers;

/// <summary>
/// Provides visual helper methods.
/// </summary>
internal static class VisualHelper
{
    internal static T? TryFindAncestor<T>(DependencyObject descendant)
        where T : class
    {
        do
        {
            if (descendant is T item)
            {
                return item;
            }

            if (descendant is not Visual)
            {
                return default;
            }

            descendant = VisualTreeHelper.GetParent(descendant);
        } while (descendant != null);

        return default;
    }
}
