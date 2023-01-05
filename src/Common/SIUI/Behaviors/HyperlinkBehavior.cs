using SIUI.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Utils;

namespace SIUI.Behaviors;

/// <summary>
/// Provides an attached hyperlink behavior to <see cref="TextBlock" />. Allows to open hyperlinks.
/// </summary>
public static class HyperlinkBehavior
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
        "Source",
        typeof(string),
        typeof(HyperlinkBehavior),
        new PropertyMetadata(null, OnSourceChanged));

    public static string GetSource(DependencyObject d) => (string)d.GetValue(SourceProperty);

    public static void SetSource(DependencyObject d, string value) => d.SetValue(SourceProperty, value);

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
        {
            return;
        }

        textBlock.Inlines.Clear();

        var newValue = (string)e.NewValue;

        if (string.IsNullOrEmpty(newValue))
        {
            return;
        }

        var previousPosition = 0;

        foreach (var match in UrlMatcher.MatchText(newValue))
        {
            if (match.Index != previousPosition)
            {
                textBlock.Inlines.Add(new Run(newValue[previousPosition..match.Index]));
            }

            var link = new Hyperlink(new Run(match.Value))
            {
                NavigateUri = Uri.TryCreate(match.Value, UriKind.Absolute, out Uri uri) ? uri : null,
                TextDecorations = null
            };

            link.Click += OnLinkClick;

            textBlock.Inlines.Add(link);

            previousPosition = match.Index + match.Length;
        }

        if (previousPosition < newValue.Length)
        {
            textBlock.Inlines.Add(new Run(newValue[previousPosition..]));
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
