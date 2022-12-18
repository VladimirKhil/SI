using System.Windows;
using System.Windows.Data;

namespace SIQuester.Utilities;

public static class CollectionViewSourceManager
{
    public static ICollectionFilter GetFilter(DependencyObject obj)
    {
        return (ICollectionFilter)obj.GetValue(FilterProperty);
    }

    public static void SetFilter(DependencyObject obj, ICollectionFilter value)
    {
        obj.SetValue(FilterProperty, value);
    }

    // Using a DependencyProperty as the backing store for Filter.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty FilterProperty =
        DependencyProperty.RegisterAttached("Filter", typeof(ICollectionFilter), typeof(CollectionViewSourceManager), new UIPropertyMetadata(null, OnFilterChanged));

    public static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var collection = d as CollectionViewSource;
        if (e.OldValue is ICollectionFilter oldValue)
        {
            collection.Filter -= oldValue.Filter;
        }
        if (e.NewValue is ICollectionFilter newValue)
        {
            collection.Filter += newValue.Filter;
        }
    }
}
