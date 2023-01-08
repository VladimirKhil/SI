using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SIGame.Behaviors;

public static class FrameBehavior
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(FrameBehavior), new PropertyMetadata(false, OnIsAttachedChanged));

    private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var frame = (Frame)d;

        frame.LoadCompleted += Frame_LoadCompleted;
        frame.DataContextChanged += Frame_DataContextChanged;
    }

    private static void Frame_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdateFrameDataContext(sender);

    private static void Frame_LoadCompleted(object sender, NavigationEventArgs e) => UpdateFrameDataContext(sender);

    private static void UpdateFrameDataContext(object sender)
    {
        var frame = (Frame)sender;

        if (frame.Content is not FrameworkElement content)
        {
            return;
        }

        content.DataContext = frame.DataContext;
    }
}
