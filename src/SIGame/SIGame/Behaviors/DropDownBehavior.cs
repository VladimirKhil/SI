using System.Windows;
using System.Windows.Controls;

namespace SIGame.Behaviors;

public static class DropDownBehavior
{
    public static ContextMenu GetDropDown(DependencyObject obj) => (ContextMenu)obj.GetValue(DropDownProperty);

    public static void SetDropDown(DependencyObject obj, ContextMenu value) => obj.SetValue(DropDownProperty, value);

    // Using a DependencyProperty as the backing store for DropDown.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DropDownProperty =
        DependencyProperty.RegisterAttached("DropDown", typeof(ContextMenu), typeof(DropDownBehavior), new PropertyMetadata(null, OnDropDownChanged));

    private static void OnDropDownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var btn = (Button)d;

        if (e.NewValue is ContextMenu ctx)
        {
            RoutedEventHandler onClick = (sender, e2) =>
            {
                ctx.PlacementTarget = btn;
                ctx.IsOpen = true;
            };

            btn.Click += onClick;
            ctx.Tag = onClick;
        }
        else if (e.OldValue is ContextMenu oldCtx)
        {
            btn.Click -= (RoutedEventHandler)oldCtx.Tag;
        }
    }
}
