using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SIGame.Controls
{
    /// <remarks>
    /// The origin of this code is unknown. Maybe it is taken from the WPF source code.
    /// TODO: detect code licence.
    /// </remarks>
    public class UniformGridWithOrientation : UniformGrid
    {
        #region Orientation (Dependency Property)   
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(System.Windows.Controls.Orientation), typeof(UniformGridWithOrientation),
                new FrameworkPropertyMetadata(
                    System.Windows.Controls.Orientation.Vertical,
                    FrameworkPropertyMetadataOptions.AffectsMeasure),
                new ValidateValueCallback(IsValidOrientation));

        internal static bool IsValidOrientation(object o)
        {
            System.Windows.Controls.Orientation orientation = (System.Windows.Controls.Orientation)o;
            if (orientation != System.Windows.Controls.Orientation.Horizontal)
            {
                return (orientation == System.Windows.Controls.Orientation.Vertical);
            }
            return true;
        }

        public System.Windows.Controls.Orientation Orientation
        {
            get { return (System.Windows.Controls.Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        #endregion

        protected override Size MeasureOverride(Size constraint)
        {
            UpdateComputedValues();
            Size availableSize = new Size(constraint.Width / ((double)_columns), constraint.Height / ((double)_rows));
            double width = 0.0;
            double height = 0.0;
            int num3 = 0;
            int count = InternalChildren.Count;
            while (num3 < count)
            {
                UIElement element = InternalChildren[num3];
                element.Measure(availableSize);
                Size desiredSize = element.DesiredSize;
                if (width < desiredSize.Width)
                {
                    width = desiredSize.Width;
                }
                if (height < desiredSize.Height)
                {
                    height = desiredSize.Height;
                }
                num3++;
            }
            return new Size(width * _columns, height * _rows);
        }
        

        private int _columns;
        private int _rows;

        private void UpdateComputedValues()
        {
            _columns = Columns;
            _rows = Rows;
            if (FirstColumn >= _columns)
            {
                FirstColumn = 0;
            }

            if (FirstColumn > 0)
                throw new NotImplementedException("There is no support for seting the FirstColumn (nor the FirstRow).");
            if ((_rows == 0) || (_columns == 0))
            {
                int num = 0;    // Visible children   
                int num2 = 0;
                int count = InternalChildren.Count;
                while (num2 < count)
                {
                    UIElement element = InternalChildren[num2];
                    if (element.Visibility != Visibility.Collapsed)
                    {
                        num++;
                    }
                    num2++;
                }
                if (num == 0)
                {
                    num = 1;
                }
                if (_rows == 0)
                {
                    if (_columns > 0)
                    {
                        _rows = ((num + FirstColumn) + (_columns - 1)) / _columns;
                    }
                    else
                    {
                        _rows = (int)Math.Sqrt((double)num);
                        if ((_rows * _rows) < num)
                        {
                            _rows++;
                        }
                        _columns = _rows;
                    }
                }
                else if (_columns == 0)
                {
                    _columns = (num + (_rows - 1)) / _rows;
                }
            }
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            Rect finalRect = new Rect(0.0, 0.0, arrangeSize.Width / ((double)_columns), arrangeSize.Height / ((double)_rows));
            double height = finalRect.Height;
            double numX = arrangeSize.Height - 1.0;
            finalRect.X += finalRect.Width * FirstColumn;
            foreach (UIElement element in InternalChildren)
            {
                element.Arrange(finalRect);
                if (element.Visibility != Visibility.Collapsed)
                {
                    finalRect.Y += height;
                    if (finalRect.Y >= numX)
                    {
                        finalRect.X += finalRect.Width;
                        finalRect.Y = 0.0;
                    }
                }
            }
            return arrangeSize;
        }
    }
}
