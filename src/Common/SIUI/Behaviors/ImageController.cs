using SIUI.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIUI.Behaviors;

public static class ImageController
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(ImageController), new PropertyMetadata(false, OnIsAttachedChanged));

    public static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var image = (Image)d;
        var tableInfo = (TableInfoViewModel?)image?.DataContext;

        if (tableInfo == null || image == null)
        {
            return;
        }

        image.ImageFailed += (sender, e2) =>
        {
            tableInfo.OnMediaLoadError(e2.ErrorException);
        };
    }
}
