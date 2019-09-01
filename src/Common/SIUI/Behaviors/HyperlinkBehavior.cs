using SIUI.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SIUI.Behaviors
{
    public static class HyperlinkBehavior
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
            "Source", typeof(string), typeof(HyperlinkBehavior), new PropertyMetadata(null, OnSourceChanged));

        public static string GetSource(DependencyObject d)
        {
            return (string)d.GetValue(SourceProperty);
        }

        public static void SetSource(DependencyObject d, string value)
        {
            d.SetValue(SourceProperty, value);
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is TextBlock textBlock))
                return;

            textBlock.Inlines.Clear();

            var newValue = (string)e.NewValue;
            if (string.IsNullOrEmpty(newValue))
                return;

            var previousPosition = 0;
            foreach (var match in UrlMatcher.MatchText(newValue))
            {
                if (match.Index != previousPosition)
                {
                    textBlock.Inlines.Add(new Run(newValue.Substring(previousPosition, match.Index - previousPosition)));
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
                textBlock.Inlines.Add(new Run(newValue.Substring(previousPosition)));
        }

        private static void OnLinkClick(object sender, RoutedEventArgs e)
        {
            var link = (Hyperlink)sender;
            try
            {
                Process.Start(link.NavigateUri.ToString());
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Error while navigating to {link.NavigateUri}: {exc.Message}");
            }
        }
    }
}
