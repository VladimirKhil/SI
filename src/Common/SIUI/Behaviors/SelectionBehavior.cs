using System.Windows;

namespace SIUI.Behaviors
{
    public static class SelectionBehavior
    {
        public static bool GetIsSelectable(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSelectableProperty);
        }

        public static void SetIsSelectable(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSelectableProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsSelectable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectableProperty =
            DependencyProperty.RegisterAttached("IsSelectable", typeof(bool), typeof(SelectionBehavior), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
    }
}
