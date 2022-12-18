using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Controls;

public sealed class BreadcrumbBar : StackPanel
{
    private double[] _newWidths = null;

    protected override Size MeasureOverride(Size constraint)
    {
        var childrenCount = Children.Count;

        var desiredWidth = 0.0;
        var height = 0.0;
        var mustWidth = constraint.Width;

        var widths = new Tuple<int, double>[childrenCount];
        var size = new Size(double.PositiveInfinity, constraint.Height);

        for (int i = 0; i < childrenCount; i++)
        {
            var child = Children[i];
            child.Measure(size);

            var width = child.DesiredSize.Width + 1.0;
            height = Math.Max(height, child.DesiredSize.Height);
            desiredWidth += width;

            widths[i] = Tuple.Create(i, width);
        }

        _newWidths = new double[childrenCount];

        if (mustWidth > 0 && desiredWidth > mustWidth)
        {
            // Переполнение, нужно ужаться
            desiredWidth = mustWidth;

            var left = childrenCount;

            foreach (var item in widths.OrderBy(t => t.Item2))
            {
                var newWidth = Math.Min(item.Item2, mustWidth / left);
                _newWidths[item.Item1] = newWidth;
                mustWidth -= newWidth;
                Children[item.Item1].Measure(new Size(newWidth, height));
                left--;
            }
        }
        else
        {
            for (int i = 0; i < childrenCount; i++)
            {
                _newWidths[i] = widths[i].Item2;
            }
        }

        return new Size(desiredWidth, height);
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        var rect = new Rect(0.0, 0.0, 0.0, arrangeBounds.Height);

        for (int i = 0; i < _newWidths.Length; i++)
        {
            rect.Width = _newWidths[i];
            Children[i].Arrange(rect);
            rect.X += _newWidths[i];
        }

        return arrangeBounds;
    }
}
