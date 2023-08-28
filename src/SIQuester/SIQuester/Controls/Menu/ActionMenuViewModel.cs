using System.Windows;

namespace SIQuester.ViewModel;

/// <summary>
/// Класс, управляющий меню действий
/// </summary>
public sealed class ActionMenuViewModel : DependencyObject
{
    public static ActionMenuViewModel Instance { get; private set; }

    static ActionMenuViewModel()
    {
        Instance = new ActionMenuViewModel();
    }

    private ActionMenuViewModel()
    {

    }
    
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set { SetValue(IsOpenProperty, value); }
    }

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register("IsOpen", typeof(bool), typeof(ActionMenuViewModel), new UIPropertyMetadata(false));

    public UIElement? PlacementTarget
    {
        get => (UIElement?)GetValue(PlacementTargetProperty);
        set { SetValue(PlacementTargetProperty, value); }
    }

    public static readonly DependencyProperty PlacementTargetProperty =
        DependencyProperty.Register("PlacementTarget", typeof(UIElement), typeof(ActionMenuViewModel), new UIPropertyMetadata(null));
}
