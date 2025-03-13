using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIQuester.Behaviors;

public static class PasteBehavior
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(PasteBehavior), new PropertyMetadata(false, IsAttachedChanged));

    public static void IsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textBox = (TextBox)d;

        if ((bool)e.NewValue)
        {
            CommandManager.AddPreviewCanExecuteHandler(textBox, OnPreviewCanExecute);
        }
        else
        {
            CommandManager.RemovePreviewCanExecuteHandler(textBox, OnPreviewCanExecute);
        }
    }

    private static void OnPreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Paste)
        {
            e.CanExecute = true;
            e.Handled = true;
        }
    }
}
