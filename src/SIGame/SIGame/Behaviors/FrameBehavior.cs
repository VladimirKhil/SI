using System.Windows;
using System.Windows.Controls;

namespace SIGame.Behaviors
{
    public static class FrameBehavior
    {
        public static bool GetIsAttached(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAttachedProperty);
        }

        public static void SetIsAttached(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAttachedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(FrameBehavior), new PropertyMetadata(false, OnIsAttachedChanged));

        private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var frame = (Frame)d;

            frame.LoadCompleted += Frame_LoadCompleted;
            frame.DataContextChanged += Frame_DataContextChanged;
        }

        private static void Frame_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateFrameDataContext(sender);
        }

        private static void Frame_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            UpdateFrameDataContext(sender);
        }

        private static void UpdateFrameDataContext(object sender)
        {
            var frame = (Frame)sender;
            var content = frame.Content as FrameworkElement;
            if (content == null)
                return;
            content.DataContext = frame.DataContext;
        }
    }
}
