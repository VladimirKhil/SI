using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SIQuester.Behaviors;

public static class LostFocusBehavior
{
    public static ToggleButton GetAttachedButton(DependencyObject obj) => (ToggleButton)obj.GetValue(AttachedButtonProperty);

    public static void SetAttachedButton(DependencyObject obj, ToggleButton value) => obj.SetValue(AttachedButtonProperty, value);

    public static readonly DependencyProperty AttachedButtonProperty =
        DependencyProperty.RegisterAttached(
            "AttachedButton",
            typeof(ToggleButton),
            typeof(LostFocusBehavior),
            new PropertyMetadata(null, OnAttachedButtonChanged));

    public static void OnAttachedButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (ToggleButton)e.NewValue;
        var textBox = (TextBox)d;

        textBox.LostKeyboardFocus += (s, e2) =>
        {
            var expression = textBox.GetBindingExpression(TextBox.TextProperty);

            if (expression != null)
            {
                expression.UpdateSource();
            }

            if (button != null)
            {
                button.IsChecked = false;
            }
        };

        Keyboard.Focus(textBox);
    }
}
