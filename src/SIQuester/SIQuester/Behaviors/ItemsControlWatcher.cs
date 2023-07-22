using System.Windows;
using System.Windows.Controls;

namespace SIQuester.ViewModel;

public sealed class ItemsControlWatcher
{
    public static bool GetIsWatching(DependencyObject obj) => (bool)obj.GetValue(IsWatchingProperty);

    public static void SetIsWatching(DependencyObject obj, bool value) => obj.SetValue(IsWatchingProperty, value);

    public static readonly DependencyProperty IsWatchingProperty =
        DependencyProperty.RegisterAttached(
            "IsWatching",
            typeof(bool),
            typeof(ItemsControlWatcher),
            new UIPropertyMetadata(false, IsWatchingChanged));

    private static void IsWatchingChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not ItemsControl element)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            element.GotFocus += Element_GotFocus;
        }
        else
        {
            element.GotFocus -= Element_GotFocus;
        }
    }

    private static void Element_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement parent || e.OriginalSource is not FrameworkElement child)
        {
            return;
        }

        var childItem = child.DataContext;

        if (parent.DataContext is not IItemsViewModel parentList || childItem == null)
        {
            if (parent.DataContext is QuestionViewModel questionViewModel && childItem != null)
            {
                // TODO: remove after switching to new format
                parentList = questionViewModel.Scenario;
            }
            else
            {
                return;
            }
        }

        if (parentList.Contains(childItem))
        {
            parentList.SetCurrentItem(childItem);
        }
    }
}
