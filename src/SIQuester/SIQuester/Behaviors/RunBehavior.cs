using System.Windows;
using System.Windows.Documents;

namespace SIQuester.Behaviors
{
    public sealed class RunBehavior: DependencyObject
    {
        public static RunBehavior Instance { get; private set; }

        static RunBehavior()
        {
            Instance = new RunBehavior();
        }

        private RunBehavior()
        {

        }

        public ContentElement PlacementTarget
        {
            get { return (ContentElement)GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PlacementTarget.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlacementTargetProperty =
            DependencyProperty.Register("PlacementTarget", typeof(ContentElement), typeof(RunBehavior), new UIPropertyMetadata(null));
        

        public static int GetDependsOn(DependencyObject obj)
        {
            return (int)obj.GetValue(DependsOnProperty);
        }

        public static void SetDependsOn(DependencyObject obj, int value)
        {
            obj.SetValue(DependsOnProperty, value);
        }

        // Using a DependencyProperty as the backing store for DependsOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DependsOnProperty =
            DependencyProperty.RegisterAttached("DependsOn", typeof(int), typeof(RunBehavior), new PropertyMetadata(0));

        public static bool GetIsEditorAttached(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEditorAttachedProperty);
        }

        public static void SetIsEditorAttached(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEditorAttachedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsEditorAttached.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditorAttachedProperty =
            DependencyProperty.RegisterAttached("IsEditorAttached", typeof(bool), typeof(RunBehavior), new PropertyMetadata(false, IsEditorAttachedChanged));

        private static void IsEditorAttachedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var run = (Run)sender;
            run.PreviewMouseLeftButtonDown += Run_MouseLeftButtonDown;
        }

        private static void Run_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var run = (Run)sender;
            Instance.PlacementTarget = run; 
        }
    }
}
