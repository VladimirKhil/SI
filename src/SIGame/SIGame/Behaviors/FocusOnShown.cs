using System.Windows;
using System.Windows.Input;

namespace SIGame.Behaviors;

public static class FocusOnShown
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(FocusOnShown), new PropertyMetadata(false, OnIsAttachedChanged));

    private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            ((FrameworkElement)d).IsVisibleChanged += FocusOnShown_IsVisibleChanged;
        }
    }

    private static void FocusOnShown_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        Keyboard.Focus((FrameworkElement)sender);
}
