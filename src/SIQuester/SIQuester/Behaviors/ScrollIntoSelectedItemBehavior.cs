using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Behaviors;

/// <summary>
/// Scrolls ListBox to show selected item.
/// </summary>
public static class ScrollIntoSelectedItemBehavior
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached(
            "IsAttached",
            typeof(bool),
            typeof(ScrollIntoSelectedItemBehavior),
            new PropertyMetadata(false, OnIsAttachedChanged));

    public static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            listBox.SelectionChanged += ListBox_SelectionChanged;
        }
        else
        {
            listBox.SelectionChanged -= ListBox_SelectionChanged;
        }
    }

    private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listBox = (ListBox)sender;
        listBox.ScrollIntoView(listBox.SelectedItem);
    }
}
