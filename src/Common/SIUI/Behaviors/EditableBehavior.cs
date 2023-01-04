using System.Windows;

namespace SIUI.Behaviors;

public static class EditableBehavior
{
    public static bool GetIsEditable(DependencyObject obj) => (bool)obj.GetValue(IsEditableProperty);

    public static void SetIsEditable(DependencyObject obj, bool value) => obj.SetValue(IsEditableProperty, value);

    public static readonly DependencyProperty IsEditableProperty =
        DependencyProperty.RegisterAttached(
            "IsEditable",
            typeof(bool),
            typeof(EditableBehavior),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
}
