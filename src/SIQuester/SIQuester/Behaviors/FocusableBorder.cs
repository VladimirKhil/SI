using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Behaviors;

public static class FocusableBorder
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(FocusableBorder), new PropertyMetadata(false, IsAttachedChanged));

    private static void IsAttachedChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var border = (Border)sender;

        border.MouseDown += (s, e2) =>
        {
            border.Focus();
            e2.Handled = true;
        };
    }
}
