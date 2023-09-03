using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace SImulator.Behaviors;

/// <summary>
/// Provides extension properties to <see cref="ListBox" />.
/// </summary>
public static class ListBoxExtensions
{
    public static IEnumerable? GetSelectedItems(DependencyObject obj) => (IEnumerable?)obj.GetValue(SelectedItemsProperty);

    public static void SetSelectedItems(DependencyObject obj, IEnumerable? value) => obj.SetValue(SelectedItemsProperty, value);

    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached("SelectedItems", typeof(IEnumerable), typeof(ListBoxExtensions), new PropertyMetadata(null, OnSelectedItemsChanged));

    public static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var listBox = (ListBox)d;
        var selectedItems = (IEnumerable?)e.NewValue;

        listBox.SelectedItems.Clear();

        if (selectedItems == null)
        {
            return;
        }

        foreach (var item in selectedItems)
        {
            listBox.SelectedItems.Add(item);
        }
    }
}