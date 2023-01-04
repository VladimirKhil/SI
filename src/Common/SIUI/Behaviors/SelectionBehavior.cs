using System.Windows;

namespace SIUI.Behaviors;

public static class SelectionBehavior
{
    public static bool GetIsSelectable(DependencyObject obj) => (bool)obj.GetValue(IsSelectableProperty);

    public static void SetIsSelectable(DependencyObject obj, bool value) => obj.SetValue(IsSelectableProperty, value);

    public static readonly DependencyProperty IsSelectableProperty =
        DependencyProperty.RegisterAttached(
            "IsSelectable",
            typeof(bool),
            typeof(SelectionBehavior),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
}
