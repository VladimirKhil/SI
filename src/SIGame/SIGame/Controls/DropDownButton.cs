using System.Windows;
using System.Windows.Controls;

namespace SIGame
{
    public sealed class DropDownButton : Button
    {
        public static readonly DependencyProperty DropDownProperty = DependencyProperty.Register("DropDown", typeof(ContextMenu), typeof(DropDownButton), new UIPropertyMetadata(null));

        public ContextMenu DropDown
        {
            get => (ContextMenu)GetValue(DropDownProperty);
            set => SetValue(DropDownProperty, value);
        }

        protected override void OnClick()
        {
            if (DropDown != null)
            {
                DropDown.PlacementTarget = this;
                DropDown.IsOpen = true;
            }
        }
    }
}
