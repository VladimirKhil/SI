using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SImulator.Behaviors;

/// <summary>
/// Selects all text in a text box when it receives focus.
/// </summary>
public static class SelectAllOnFocusBehavior
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(SelectAllOnFocusBehavior), new PropertyMetadata(false, IsAttachedChanged));

    private static void IsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textBox = (TextBox)d;

        if ((bool)e.NewValue)
        {
            textBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
            textBox.Loaded += TextBox_Loaded;
            textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
        }
        else
        {
            textBox.GotKeyboardFocus -= TextBox_GotKeyboardFocus;
            textBox.Loaded -= TextBox_Loaded;
            textBox.PreviewMouseLeftButtonDown -= TextBox_PreviewMouseLeftButtonDown;
        }
    }

    private static void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ((TextBox)sender).SelectAll();

    private static void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var textBox = (TextBox)sender;

        if (!textBox.IsKeyboardFocusWithin)
        {
            textBox.Focus();
            e.Handled = true;
        }
    }

    private static void TextBox_Loaded(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;

        textBox.Dispatcher.BeginInvoke(
            new Action(() =>
            {
                if (textBox.IsKeyboardFocusWithin)
                {
                    textBox.SelectAll();
                }
            }),
            DispatcherPriority.Input);
    }
}
