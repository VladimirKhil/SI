using System.Windows;
using System.Windows.Documents;
using Utils;

namespace SIQuester.Behaviors;

/// <summary>
/// Provides an attached hyperlink behavior to <see cref="Hyperlink" />. Allows to open hyperlinks.
/// </summary>
public static class HyperlinkBehavior
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(HyperlinkBehavior), new PropertyMetadata(false, IsAttachedChanged));

    private static void IsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Hyperlink hyperlink)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            hyperlink.Click += OnLinkClick;
        }
        else
        {
            hyperlink.Click -= OnLinkClick;
        }
    }

    private static void OnLinkClick(object sender, RoutedEventArgs e)
    {
        var link = (Hyperlink)sender;

        try
        {
            Browser.Open(link.NavigateUri.ToString());
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }
}
