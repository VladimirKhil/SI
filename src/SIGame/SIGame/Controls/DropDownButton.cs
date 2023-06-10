using SIGame.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SIGame;

/// <summary>
/// Represents a button that shows drop-down menu on click.
/// </summary>
public sealed class DropDownButton : Button
{
    public static readonly DependencyProperty DropDownProperty =
        DependencyProperty.Register("DropDown", typeof(ContextMenu), typeof(DropDownButton), new UIPropertyMetadata(null));

    /// <summary>
    /// Button drop-down menu.
    /// </summary>
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

        DropDown.PlacementTarget = this;

        try
        {
            DropDown.IsOpen = true;
        }
        catch (DivideByZeroException) // at void System.Windows.Vector.Normalize()
        {
            MessageBox.Show("DropDownButton Error. Contact game author", AppConstants.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }
}
