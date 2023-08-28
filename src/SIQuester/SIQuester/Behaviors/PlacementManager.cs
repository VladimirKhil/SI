using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SIQuester.Behaviors;

public static class PlacementManager
{
    public static UIElement? GetPlacementTarget(DependencyObject obj) => (UIElement)obj.GetValue(PlacementTargetProperty);

    public static void SetPlacementTarget(DependencyObject obj, UIElement? value) => obj.SetValue(PlacementTargetProperty, value);

    public static readonly DependencyProperty PlacementTargetProperty =
        DependencyProperty.RegisterAttached(
            "PlacementTarget",
            typeof(UIElement),
            typeof(PlacementManager),
            new PropertyMetadata(null, OnPlacementTargetChanged));

    public static void OnPlacementTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue == null)
        {
            return;
        }

        var grid = (FrameworkElement)VisualTreeHelper.GetParent(d);
        var newPlacement = (UIElement)e.NewValue;
        var margin = newPlacement.TranslatePoint(new Point(0, 0), grid);

        var newX = Math.Min(margin.X + (margin.Y == 0 ? 400 : 100), grid.ActualWidth - 300);
        var newY = Math.Max(0, margin.Y - 90);

        var sb = new Storyboard();
        Storyboard.SetTarget(sb, d);
        Storyboard.SetTargetProperty(sb, new PropertyPath("Margin"));
        sb.Children.Add(new ThicknessAnimation(new Thickness(newX, newY, 0, 0), new Duration(TimeSpan.FromSeconds(0.1))));
        sb.Begin();
    }
}
