using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SIQuester.Behaviors;

/// <summary>
/// Shows a semi-transparent hint in an empty text box until something is typed.
/// </summary>
/// <example><c>lb:Placeholder.Text="{x:Static lp:Resources.ContactUriHint}"</c></example>
public static class Placeholder
{
    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);

    public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(Placeholder), new PropertyMetadata("", OnTextChanged));

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
        {
            return;
        }

        textBox.Loaded -= TextBox_Loaded;
        textBox.Loaded += TextBox_Loaded;

        if (textBox.IsLoaded)
        {
            Attach(textBox);
        }
    }

    private static void TextBox_Loaded(object sender, RoutedEventArgs e) => Attach((TextBox)sender);

    private static void Attach(TextBox textBox)
    {
        var layer = AdornerLayer.GetAdornerLayer(textBox);

        if (layer == null || layer.GetAdorners(textBox)?.OfType<PlaceholderAdorner>().Any() == true)
        {
            return;
        }

        var adorner = new PlaceholderAdorner(textBox, GetText(textBox));
        layer.Add(adorner);

        void Refresh(object? sender, EventArgs e) => adorner.InvalidateVisual();

        void RefreshOnVisibility(object? sender, DependencyPropertyChangedEventArgs e) => adorner.InvalidateVisual();

        // A collapsed theme or a removed item must not leave the hint in the old place.
        // When the text box is loaded again, TextBox_Loaded attaches a new hint
        void Detach(object? sender, RoutedEventArgs e)
        {
            textBox.TextChanged -= Refresh;
            textBox.IsVisibleChanged -= RefreshOnVisibility;
            textBox.Unloaded -= Detach;
            layer.Remove(adorner);
        }

        textBox.TextChanged += Refresh;
        textBox.IsVisibleChanged += RefreshOnVisibility;
        textBox.Unloaded += Detach;
    }

    private sealed class PlaceholderAdorner : Adorner
    {
        private readonly TextBox _textBox;
        private readonly string _text;

        public PlaceholderAdorner(TextBox textBox, string text) : base(textBox)
        {
            _textBox = textBox;
            _text = text;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_textBox.Text.Length > 0 || string.IsNullOrEmpty(_text) || !_textBox.IsVisible)
            {
                return;
            }

            // The field's own text color: readable in every theme. The hint may go past the right edge of a narrow field
            var formatted = new FormattedText(
                _text,
                CultureInfo.CurrentUICulture,
                _textBox.FlowDirection,
                new Typeface(_textBox.FontFamily, _textBox.FontStyle, _textBox.FontWeight, _textBox.FontStretch),
                _textBox.FontSize,
                _textBox.Foreground,
                VisualTreeHelper.GetDpi(_textBox).PixelsPerDip);

            var x = _textBox.Padding.Left + _textBox.BorderThickness.Left + 3;
            var y = Math.Max(0, (_textBox.ActualHeight - formatted.Height) / 2);

            drawingContext.PushOpacity(0.5);
            drawingContext.DrawText(formatted, new Point(x, y));
            drawingContext.Pop();
        }
    }
}
