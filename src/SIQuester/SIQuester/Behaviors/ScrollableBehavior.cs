using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Behaviors;

/// <summary>
/// Allows RichTextBox to scroll to end when its text changed.
/// </summary>
internal static class ScrollableBehavior
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(ScrollableBehavior), new UIPropertyMetadata(false, PropertyChanged));

    private static void PropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var richTextBox = (RichTextBox)sender;

        if ((bool)e.NewValue)
        {
            richTextBox.TextChanged += RichTextBox_TextChanged;
        }
        else
        {
            richTextBox.TextChanged -= RichTextBox_TextChanged;
        }
    }

    private static void RichTextBox_TextChanged(object sender, TextChangedEventArgs e) => ((RichTextBox)sender).ScrollToEnd();
}
