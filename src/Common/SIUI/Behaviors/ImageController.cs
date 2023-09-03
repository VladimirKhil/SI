using SIUI.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIUI.Behaviors;

/// <summary>
/// Allows to attach load handler to Image control.
/// </summary>
public static class ImageController
{
    public static TableInfoViewModel? GetLoadHandler(DependencyObject obj) => (TableInfoViewModel?)obj.GetValue(LoadHandlerProperty);

    public static void SetLoadHandler(DependencyObject obj, TableInfoViewModel? value) => obj.SetValue(LoadHandlerProperty, value);

    public static readonly DependencyProperty LoadHandlerProperty =
        DependencyProperty.RegisterAttached("LoadHandler", typeof(TableInfoViewModel), typeof(ImageController), new PropertyMetadata(null, OnLoadHandlerChanged));

    public static void OnLoadHandlerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var image = (Image)d;
        var tableInfo = (TableInfoViewModel?)e.NewValue;

        if (tableInfo == null || image == null)
        {
            return;
        }

        image.Loaded += (sender, e2) =>
        {
            tableInfo.OnMediaLoad();
        };

        image.ImageFailed += (sender, e2) =>
        {
            tableInfo.OnMediaLoadError(e2.ErrorException);
        };
    }
}
