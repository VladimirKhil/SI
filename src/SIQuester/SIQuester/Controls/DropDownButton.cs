using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SIQuester;

/// <summary>
/// Defines a button with drop down context menu.
/// </summary>
public class DropDownButton : Button
{
    public static readonly DependencyProperty DropDownProperty =
        DependencyProperty.Register("DropDown", typeof(ContextMenu), typeof(DropDownButton), new UIPropertyMetadata(null));

    public ContextMenu DropDown
    {
        get => (ContextMenu)GetValue(DropDownProperty);
        set => SetValue(DropDownProperty, value);
    }

    protected override void OnClick()
    {
        if (DropDown == null)
        {
            return;
        }

        // If there is a drop-down assigned to this button, then position and display it 

        DropDown.PlacementTarget = this;
        DropDown.Placement = PlacementMode.Bottom;
        DropDown.IsOpen = true;
    }

}
