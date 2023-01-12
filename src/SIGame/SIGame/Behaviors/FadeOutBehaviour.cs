using SIGame.ViewModel.Data;
using System.Windows;
using System.Windows.Media.Animation;

namespace SIGame.Behaviors;

public static class FadeOutBehaviour
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(FadeOutBehaviour), new PropertyMetadata(false, OnIsAttachedChanged));

    private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (((FrameworkElement)d).DataContext is ICloseable dataContext)
        {
            dataContext.Closed += () =>
            {
                var storyboard = (Storyboard)Application.Current.Resources["FadeOut"];
                storyboard.Begin((FrameworkElement)d);
            };
        }
    }
}
