using System.Windows;

namespace SIGame.Behaviors;

public static class Dragger
{
    public static bool GetIsDragged(DependencyObject obj) => (bool)obj.GetValue(IsDraggedProperty);

    public static void SetIsDragged(DependencyObject obj, bool value) => obj.SetValue(IsDraggedProperty, value);

    // Using a DependencyProperty as the backing store for IsDragged.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsDraggedProperty =
        DependencyProperty.RegisterAttached("IsDragged", typeof(bool), typeof(Dragger), new UIPropertyMetadata(false));

    public static double GetDragPosition(DependencyObject obj) => (double)obj.GetValue(DragPositionProperty);

    public static void SetDragPosition(DependencyObject obj, double value) => obj.SetValue(DragPositionProperty, value);

    // Using a DependencyProperty as the backing store for DragPosition.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DragPositionProperty =
        DependencyProperty.RegisterAttached("DragPosition", typeof(double), typeof(Dragger), new UIPropertyMetadata(0.0));
}
