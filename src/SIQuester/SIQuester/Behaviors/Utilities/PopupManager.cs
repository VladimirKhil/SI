using System.Windows;
using System.Windows.Controls.Primitives;

namespace SIQuester.Utilities;

public sealed class PopupManager
{
    public static Window GetOwner(DependencyObject obj) => (Window)obj.GetValue(OwnerProperty);

    public static void SetOwner(DependencyObject obj, Window value) => obj.SetValue(OwnerProperty, value);

    public static readonly DependencyProperty OwnerProperty =
        DependencyProperty.RegisterAttached("Owner", typeof(Window), typeof(PopupManager), new UIPropertyMetadata(null, OnOwnerChanged));

    public static bool? GetWasOpened(DependencyObject obj) => (bool?)obj.GetValue(WasOpenedProperty);

    public static void SetWasOpened(DependencyObject obj, bool? value) => obj.SetValue(WasOpenedProperty, value);

    public static readonly DependencyProperty WasOpenedProperty =
        DependencyProperty.RegisterAttached("WasOpened", typeof(bool?), typeof(PopupManager), new UIPropertyMetadata(null));
    
    public static void OnOwnerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var popup = d as Popup;

        void act(object sender, EventArgs e2)
        {
            var window = sender as Window;
            if (window.IsActive && window.WindowState != WindowState.Minimized)
            {
                if (GetWasOpened(popup).HasValue)
                    popup.IsOpen = GetWasOpened(popup).Value;

                SetWasOpened(popup, null);
            }
            else
                popup.IsOpen = false;
        }

        void state(object sender, EventArgs e2)
        {
            var window = sender as Window;
            if (window.IsActive && window.WindowState != WindowState.Minimized)
            {
                if (GetWasOpened(popup).HasValue)
                    popup.IsOpen = GetWasOpened(popup).Value;
                SetWasOpened(popup, null);
            }
            else if (!GetWasOpened(popup).HasValue)
            {
                SetWasOpened(popup, popup.IsOpen);
                popup.IsOpen = false;
            }
        }

        void deact(object sender, EventArgs e2)
        {
            if (!GetWasOpened(popup).HasValue)
            {
                SetWasOpened(popup, popup.IsOpen);
                popup.IsOpen = false;
            }
        }

        if (e.OldValue is Window oldValue)
        {
            oldValue.Activated -= act;
            oldValue.Deactivated -= deact;
            oldValue.StateChanged -= state;
        }

        if (e.NewValue is Window newValue)
        {
            newValue.Activated += act;
            newValue.Deactivated += deact;
            newValue.StateChanged += state;
        }
    }
}
