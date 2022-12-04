using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SImulator.Behaviors;

public static class TipBehavior
{
    public static UIElement GetTip(DependencyObject obj) => (UIElement)obj.GetValue(TipProperty);

    public static void SetTip(DependencyObject obj, UIElement value) => obj.SetValue(TipProperty, value);

    public static readonly DependencyProperty TipProperty =
        DependencyProperty.RegisterAttached("Tip", typeof(UIElement), typeof(TipBehavior), new PropertyMetadata(null, TipChanged));

    private static void TipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (Button)d;

        var popup = new Popup
        {
            Focusable = true,
            Child = new Border
            {
                Background = Brushes.WhiteSmoke, 
                BorderThickness = new Thickness(1.0),
                BorderBrush = Brushes.Gray,
                Padding = new Thickness(6.0, 3.0, 6.0, 3.0),
                Child = (UIElement)e.NewValue,
                MaxWidth = 400.0
            },
            PlacementTarget = button,
            Placement = PlacementMode.Mouse,
            StaysOpen = false
        };

        TextBlock.SetFontSize(popup, 12.0);

        popup.LostFocus += (sender2, e3) =>
        {
            popup.IsOpen = false;
        };

        button.Click += (sender, e2) =>
        {
            popup.IsOpen = true;
        };
    }
}
