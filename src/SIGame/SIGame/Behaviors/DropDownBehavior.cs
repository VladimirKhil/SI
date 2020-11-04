using System.Windows;
using System.Windows.Controls;

namespace SIGame.Behaviors
{
    public static class DropDownBehavior
    {
        public static ContextMenu GetDropDown(DependencyObject obj)
        {
            return (ContextMenu)obj.GetValue(DropDownProperty);
        }

        public static void SetDropDown(DependencyObject obj, ContextMenu value)
        {
            obj.SetValue(DropDownProperty, value);
        }

        // Using a DependencyProperty as the backing store for DropDown.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DropDownProperty =
            DependencyProperty.RegisterAttached("DropDown", typeof(ContextMenu), typeof(DropDownBehavior), new PropertyMetadata(null, OnDropDownChanged));

        private static void OnDropDownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var btn = (Button)d;
            var ctx = e.NewValue as ContextMenu;

            if (ctx != null)
            {
                RoutedEventHandler onClick = (sender, e2) =>
                {
                    ctx.PlacementTarget = btn;
                    ctx.IsOpen = true;
                };

                btn.Click += onClick;
                ctx.Tag = onClick;
            }
            else
            {
                ctx = e.OldValue as ContextMenu;
                btn.Click -= (RoutedEventHandler)ctx.Tag;
            }
        }
    }
}
