using SIUI.Converters;
using SIUI.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace SIUI;

/// <summary>
/// Defines Table.xaml interaction logic.
/// </summary>
public partial class Table : UserControl
{
    /// <summary>
    /// Animation finish flag.
    /// </summary>
    public bool Finished
    {
        get => (bool)GetValue(FinishedProperty);
        set => SetValue(FinishedProperty, value);
    }

    public static readonly DependencyProperty FinishedProperty =
        DependencyProperty.Register("Finished", typeof(bool), typeof(Table), new UIPropertyMetadata(false));
    
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
        ConfigureSelections();
        ConfigureZoom();
    }

    private void ConfigureSelections()
    {
        var qSelection = FindResource("QSelection") as Timeline ?? throw new InvalidOperationException("QSelection resource not found");
        qSelection.Completed += Selection_Completed;

        var tSelection = FindResource("TSelection") as Timeline ?? throw new InvalidOperationException("TSelection resource not found");
        tSelection.Completed += Selection_Completed;
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

    private void Selection_Completed(object? sender, EventArgs e) => Finished = true;

    private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is TableInfoViewModel data)
        {
            SetBinding(FinishedProperty, new Binding(nameof(Finished)) { Source = data, Mode = BindingMode.TwoWay });
        }
    }
}
