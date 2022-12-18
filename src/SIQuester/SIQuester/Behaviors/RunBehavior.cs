using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace SIQuester.Behaviors;

public sealed class RunBehavior : DependencyObject
{
    public static RunBehavior Instance { get; } = new RunBehavior();

    private RunBehavior() { }

    public ContentElement PlacementTarget
    {
        get => (ContentElement)GetValue(PlacementTargetProperty);
        set { SetValue(PlacementTargetProperty, value); }
    }

    public static readonly DependencyProperty PlacementTargetProperty =
        DependencyProperty.Register("PlacementTarget", typeof(ContentElement), typeof(RunBehavior), new UIPropertyMetadata(null));


    public static int GetDependsOn(DependencyObject obj) => (int)obj.GetValue(DependsOnProperty);

    public static void SetDependsOn(DependencyObject obj, int value) => obj.SetValue(DependsOnProperty, value);

    public static readonly DependencyProperty DependsOnProperty =
        DependencyProperty.RegisterAttached("DependsOn", typeof(int), typeof(RunBehavior), new PropertyMetadata(0));

    public static bool GetIsEditorAttached(DependencyObject obj) => (bool)obj.GetValue(IsEditorAttachedProperty);

    public static void SetIsEditorAttached(DependencyObject obj, bool value) => obj.SetValue(IsEditorAttachedProperty, value);

    public static readonly DependencyProperty IsEditorAttachedProperty =
        DependencyProperty.RegisterAttached(
            "IsEditorAttached",
            typeof(bool),
            typeof(RunBehavior),
            new PropertyMetadata(false, IsEditorAttachedChanged));

    private static void IsEditorAttachedChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var run = (Run)sender;
        run.PreviewMouseLeftButtonDown += Run_MouseLeftButtonDown;
    }

    private static void Run_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var run = (Run)sender;
        Instance.PlacementTarget = run; 
    }
}
