using System.Windows;
using System.Windows.Controls;

namespace SIUI.Behaviors;

public static class LoopBehavior
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(LoopBehavior), new PropertyMetadata(false, OnIsAttachedChanged));

    public static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var mediaElement = (MediaElement)d;
        mediaElement.LoadedBehavior = MediaState.Manual;
        mediaElement.Loaded += (sender, e2) =>
        {
            mediaElement.Play();
        };

        mediaElement.MediaEnded += (sender, e2) =>
        {
            mediaElement.Position = TimeSpan.Zero;
            mediaElement.Play();
        };
    }
}
