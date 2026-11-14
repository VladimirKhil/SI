using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SIQuester.Behaviors;

public static class PlacementManager
{
    /// <summary>
    /// Layout handlers of the toolbars that are on screen. A toolbar is removed when it is unloaded
    /// (document closed or another tab selected), so closed documents are not kept in memory.
    /// </summary>
    private static readonly Dictionary<FrameworkElement, EventHandler> HookedToolbars = new();

    private static void Hook(FrameworkElement toolbar)
    {
        if (HookedToolbars.ContainsKey(toolbar))
        {
            return;
        }

        void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (GetPlacementTarget(toolbar) is UIElement current && toolbar.IsVisible)
            {
                Place(toolbar, current, allowRetry: false);
            }
        }

        void OnUnloaded(object sender, RoutedEventArgs e)
        {
            toolbar.Unloaded -= OnUnloaded;
            toolbar.LayoutUpdated -= OnLayoutUpdated;
            HookedToolbars.Remove(toolbar);
            LastSpots.Remove(toolbar);

            // Shown again later (tab selected back): start following the layout again
            void OnLoadedAgain(object s, RoutedEventArgs a)
            {
                toolbar.Loaded -= OnLoadedAgain;

                if (GetPlacementTarget(toolbar) is UIElement current)
                {
                    Place(toolbar, current, allowRetry: true);
                }
            }

            toolbar.Loaded += OnLoadedAgain;
        }

        HookedToolbars[toolbar] = OnLayoutUpdated;
        toolbar.LayoutUpdated += OnLayoutUpdated;
        toolbar.Unloaded += OnUnloaded;
    }

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
        if (e.NewValue is UIElement target)
        {
            Place(d, target, allowRetry: true);
        }
    }

    private static void Place(DependencyObject d, UIElement newPlacement, bool allowRetry)
    {
        var toolbar = (FrameworkElement)d;

        if (VisualTreeHelper.GetParent(d) is not FrameworkElement grid)
        {
            // At startup the target is set before the toolbar is in the visual tree: place it once it is loaded
            void OnLoaded(object sender, RoutedEventArgs args)
            {
                toolbar.Loaded -= OnLoaded;

                if (GetPlacementTarget(toolbar) is UIElement current)
                {
                    Place(toolbar, current, allowRetry: true);
                }
            }

            toolbar.Loaded += OnLoaded;
            return;
        }

        // Layout keeps changing after startup (the document column and the tree items get their final sizes,
        // themes expand, the toolbar gets its second row): re-check after every layout pass.
        // AnimateTo skips the move when the spot did not change, so this is cheap.
        if (allowRetry)
        {
            Hook(toolbar);
        }

        var margin = newPlacement.TranslatePoint(new Point(0, 0), grid);
        var toolbarWidth = toolbar.ActualWidth > 0 ? toolbar.ActualWidth : 640;

        // Sit fully above the target: use the real toolbar height (two rows are taller than the old fixed 90 px)
        var toolbarHeight = Math.Max(toolbar.ActualHeight, 96);

        // Fields inside a question (answers, content, comments): go above the whole question row, so nothing in it is covered
        var row = newPlacement is System.Windows.Controls.TreeViewItem ? null : FindAncestor<System.Windows.Controls.TreeViewItem>(newPlacement);
        var top = row != null ? row.TranslatePoint(new Point(0, 0), grid).Y : margin.Y;

        double newX;
        var targetWidth = (newPlacement as FrameworkElement)?.ActualWidth ?? 0;

        // Package header card: the toolbar sits inside the card's empty top-right corner (nothing moves)
        var cardItem = newPlacement as System.Windows.Controls.TreeViewItem ?? row;

        if (cardItem != null && Equals(cardItem.Tag, "Card"))
        {
            var cardOrigin = cardItem.TranslatePoint(new Point(0, 0), grid);
            const double CardMaxWidth = 1160; // see the package HierarchicalDataTemplate
            var cardRight = cardOrigin.X + Math.Min(cardItem.ActualWidth, CardMaxWidth + 40);

            AnimateTo(d, Math.Max(0, cardRight - toolbarWidth - 30), Math.Max(0, cardOrigin.Y + 16));
            return;
        }

        if (row != null && margin.X + targetWidth / 2 > grid.ActualWidth / 2)
        {
            // Target on the right (answers): right edge of the toolbar follows the right edge of the target
            newX = margin.X + targetWidth - toolbarWidth;
        }
        else
        {
            // Past the theme/round title and its badge on the left, so the row above stays readable
            newX = margin.X + 480;
        }

        newX = Math.Max(0, Math.Min(newX, grid.ActualWidth - toolbarWidth));
        var newY = Math.Max(0, top - toolbarHeight - 4);

        AnimateTo(d, newX, newY);
    }

    private static readonly Dictionary<DependencyObject, Point> LastSpots = new();

    private static void AnimateTo(DependencyObject toolbar, double x, double y)
    {
        // Already there (or already moving there): nothing to do
        if (LastSpots.TryGetValue(toolbar, out var last) && Math.Abs(last.X - x) < 1 && Math.Abs(last.Y - y) < 1)
        {
            return;
        }

        LastSpots[toolbar] = new Point(x, y);

        var sb = new Storyboard();
        Storyboard.SetTarget(sb, toolbar);
        Storyboard.SetTargetProperty(sb, new PropertyPath("Margin"));
        sb.Children.Add(new ThicknessAnimation(new Thickness(x, y, 0, 0), new Duration(TimeSpan.FromSeconds(0.1))));
        sb.Begin();
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        element = element != null ? VisualTreeHelper.GetParent(element) : null;

        while (element != null)
        {
            if (element is T found)
            {
                return found;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return null;
    }
}
