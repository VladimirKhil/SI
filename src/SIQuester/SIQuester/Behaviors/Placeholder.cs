using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SIQuester.Behaviors;

/// <summary>
/// Gray hint text shown in an empty text box (or editable combo box) until something is typed.
/// Usage: <c>lb:Placeholder.Text="..."</c>.
/// </summary>
public static class Placeholder
{
    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);

    public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(Placeholder), new PropertyMetadata("", OnTextChanged));

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        element.Loaded -= Element_Loaded;
        element.Loaded += Element_Loaded;

        if (element.IsLoaded)
        {
            Attach(element);
        }
    }

    private static void Element_Loaded(object sender, RoutedEventArgs e) => Attach((FrameworkElement)sender);

    private static void Attach(FrameworkElement element)
    {
        // Editable ComboBox: the hint goes into its inner text box
        var textBox = element as TextBox
            ?? (element as ComboBox)?.Template?.FindName("PART_EditableTextBox", element) as TextBox;

        if (textBox == null)
        {
            return;
        }

        var layer = AdornerLayer.GetAdornerLayer(textBox);

        if (layer == null || layer.GetAdorners(textBox)?.OfType<PlaceholderAdorner>().Any() == true)
        {
            return;
        }

        var adorner = new PlaceholderAdorner(textBox, GetText(element));
        layer.Add(adorner);

        void Refresh(object? s, EventArgs a) => adorner.InvalidateVisual();

        void RefreshVisibility(object? s, DependencyPropertyChangedEventArgs a) => adorner.InvalidateVisual();

        // A collapsed theme or a removed item must not leave the hint hanging in the old place
        void Detach(object? s, RoutedEventArgs a)
        {
            textBox.TextChanged -= Refresh;
            textBox.GotKeyboardFocus -= Refresh;
            textBox.LostKeyboardFocus -= Refresh;
            textBox.IsVisibleChanged -= RefreshVisibility;
            textBox.Unloaded -= Detach;
            layer.Remove(adorner);
        }

        textBox.TextChanged += Refresh;
        textBox.GotKeyboardFocus += Refresh;
        textBox.LostKeyboardFocus += Refresh;
        textBox.IsVisibleChanged += RefreshVisibility;
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

            var brush = _textBox.TryFindResource("Ui.Muted") as Brush ?? Brushes.Gray;

            var formatted = new FormattedText(
                _text,
                CultureInfo.CurrentUICulture,
                _textBox.FlowDirection,
                new Typeface(_textBox.FontFamily, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal),
                _textBox.FontSize,
                brush,
                VisualTreeHelper.GetDpi(_textBox).PixelsPerDip)
            {
                MaxTextWidth = Math.Max(1, _textBox.ActualWidth - _textBox.Padding.Left - _textBox.Padding.Right - 6),
                MaxLineCount = 1,
                Trimming = TextTrimming.CharacterEllipsis,
            };

            var x = _textBox.Padding.Left + _textBox.BorderThickness.Left + 3;
            var y = Math.Max(0, (_textBox.ActualHeight - formatted.Height) / 2);

            drawingContext.PushOpacity(0.75);
            drawingContext.DrawText(formatted, new Point(x, y));
            drawingContext.Pop();
        }
    }
}
