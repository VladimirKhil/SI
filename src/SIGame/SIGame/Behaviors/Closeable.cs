using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SIGame.Behaviors;

public static class Closeable
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(Closeable), new PropertyMetadata(false, OnIsAttachedChanged));

    private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Button)d).PreviewMouseUp += Closeable_PreviewMouseUp;

    private static void Closeable_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var parent = (FrameworkElement)(Button)sender;

        while (true)
        {
            parent = (FrameworkElement)VisualTreeHelper.GetParent(parent);

            if (parent == null)
            {
                return;
            }

            if (parent is ContextMenu contextMenu)
            {
                contextMenu.IsOpen = false;
                return;
            }
        }
    }
}
