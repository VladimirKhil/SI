using System.Windows;
using System.Windows.Controls;

namespace SIQuester.ViewModel
{
    public sealed class ItemsControlWatcher
    {
        public static bool GetIsWatching(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsWatchingProperty);
        }

        public static void SetIsWatching(DependencyObject obj, bool value)
        {
            obj.SetValue(IsWatchingProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsWatching.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsWatchingProperty =
            DependencyProperty.RegisterAttached("IsWatching", typeof(bool), typeof(ItemsControlWatcher), new UIPropertyMetadata(false, IsWatchingChanged));

        private static void IsWatchingChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ItemsControl element)
            {
                var parentList = element.DataContext as IItemsViewModel;

                if ((bool)e.NewValue)
                {
                    element.GotFocus += Element_GotFocus;
                }
                else
                {
                    element.GotFocus -= Element_GotFocus;
                }
            }
        }

        private static void Element_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement parent) || !(e.OriginalSource is FrameworkElement child))
            {
                return;
            }

            var childItem = child.DataContext;

            if (!(parent.DataContext is IItemsViewModel parentList) || childItem == null)
            {
                return;
            }

            if (parentList.Contains(childItem))
            {
                parentList.SetCurrentItem(childItem);
            }
        }
    }
}
