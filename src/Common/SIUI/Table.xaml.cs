using SIUI.Converters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SIUI;

/// <summary>
/// Defines Table.xaml interaction logic.
/// </summary>
public partial class Table : UserControl
{
    /// <summary>
    /// Zoom value.
    /// </summary>
    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register("Zoom", typeof(double), typeof(Table), new UIPropertyMetadata(1.0));

    public Table()
    {
        InitializeComponent();
        ConfigureZoom();
    }

    private void ConfigureZoom()
    {
        var widthBinding = new Binding("ActualWidth") { Source = this };
        var heightBinding = new Binding("ActualHeight") { Source = this };
        var zoomBinding = new MultiBinding { Converter = new ZoomConverter { BaseWidth = 400, BaseHeight = 300 } };

        zoomBinding.Bindings.Add(widthBinding);
        zoomBinding.Bindings.Add(heightBinding);
        SetBinding(ZoomProperty, zoomBinding);
    }
}
