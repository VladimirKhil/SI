using System;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Implementation;

internal static class ScrollHelper
{
    private const double ScrollAreaHeight = 40.0;

    internal static void ScrollView(DragEventArgs e, ScrollViewer scrollViewer = null)
    {
        var scroller = scrollViewer ?? FlatDocView.FindAncestor<ScrollViewer>((DependencyObject)e.OriginalSource);

        if (scroller == null)
        {
            return;
        }

        var pos = e.GetPosition(scroller);

        double offsetDelta = 0.0;

        // See if we need to scroll 
        if (scroller.ViewportHeight - pos.Y < ScrollAreaHeight)
        {
            offsetDelta = ScrollAreaHeight - (scroller.ViewportHeight - pos.Y);
        }
        else if (pos.Y < ScrollAreaHeight)
        {
            offsetDelta = pos.Y - ScrollAreaHeight;
        }

        // Scroll the tree down or up 
        if (Math.Abs(offsetDelta) > 0)
        {
            var newOffset = Math.Max(0, Math.Min(scroller.ScrollableHeight, scroller.VerticalOffset + offsetDelta));
            scroller.ScrollToVerticalOffset(newOffset);
        }
    }
}
