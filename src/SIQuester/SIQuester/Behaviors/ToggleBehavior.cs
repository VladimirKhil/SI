using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SIQuester.Behaviors;

public static class ToggleBehavior
{
    public static ToggleButton GetTarget(DependencyObject obj) => (ToggleButton)obj.GetValue(TargetProperty);

    public static void SetTarget(DependencyObject obj, ToggleButton value) => obj.SetValue(TargetProperty, value);

    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.RegisterAttached("Target", typeof(ToggleButton), typeof(ToggleBehavior), new PropertyMetadata(null, OnTargetChanged));

    public static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue == null)
        {
            return;
        }

        var button = (Button)d;

        button.Click += (s, e1) =>
        {
            ((ToggleButton)e.NewValue).IsChecked = false;
        };
    }
}
