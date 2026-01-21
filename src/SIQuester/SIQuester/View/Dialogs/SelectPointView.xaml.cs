using SIQuester.ViewModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SIQuester.View.Dialogs;

public partial class SelectPointView : Window
{
    private readonly SIPackages.ContentItem _contentItem;
    private readonly PointAnswerViewModel _viewModel;
    private BitmapImage? _bitmap;
    private string _currentAnswer = "";

    public double CurrentDeviation
    {
        get { return (double)GetValue(CurrentDeviationProperty); }
        set { SetValue(CurrentDeviationProperty, value); }
    }

    public static readonly DependencyProperty CurrentDeviationProperty =
        DependencyProperty.Register("CurrentDeviation", typeof(double), typeof(SelectPointView), new PropertyMetadata(0.0, OnCurrentDeviationChanged, CoerceCurrentDeviation));

    private static object CoerceCurrentDeviation(DependencyObject d, object baseValue)
    {
        var val = (double)baseValue;
        if (val < 0.0) return 0.0;
        if (val > 0.5) return 0.5;
        return val;
    }

    private static void OnCurrentDeviationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SelectPointView)d).UpdateVisuals();
    }

    public SelectPointView(SIPackages.ContentItem contentItem, PointAnswerViewModel viewModel)
    {
        InitializeComponent();
        _contentItem = contentItem;
        _viewModel = viewModel;
        CurrentDeviation = _viewModel.Deviation;
        _currentAnswer = _viewModel.Answer;
        DataContext = _viewModel;
        Loaded += SelectPointView_Loaded;
    }

    private void SelectPointView_Loaded(object sender, RoutedEventArgs e)
    {
        LoadImage();
        UpdateVisuals();
    }

    private void LoadImage()
    {
        try 
        {
            var streamInfo = _viewModel.GetImageStream(_contentItem.Value);
            if (streamInfo != null)
            {
                using (streamInfo.Stream)
                {
                    if (streamInfo.Stream.CanSeek)
                    {
                        streamInfo.Stream.Seek(0, SeekOrigin.Begin);
                    }

                    using var mem = new MemoryStream();
                    streamInfo.Stream.CopyTo(mem);
                    mem.Position = 0;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = mem;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    _bitmap = bitmap;
                    TargetImage.Source = _bitmap;
                }
            }
        }
        catch (Exception) { }
    }

    private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_bitmap == null) return;

        var pos = e.GetPosition(ReferenceGrid);
        
        var scaleX = ReferenceGrid.ActualWidth / _bitmap.PixelWidth;
        var scaleY = ReferenceGrid.ActualHeight / _bitmap.PixelHeight;

        double imageWidth, imageHeight, left, top;
        
        if (scaleX < scaleY)
        {
            imageWidth = ReferenceGrid.ActualWidth;
            imageHeight = _bitmap.PixelHeight * scaleX;
            left = 0;
            top = (ReferenceGrid.ActualHeight - imageHeight) / 2;
        }
        else
        {
            imageWidth = _bitmap.PixelWidth * scaleY;
            imageHeight = ReferenceGrid.ActualHeight;
            left = (ReferenceGrid.ActualWidth - imageWidth) / 2;
            top = 0;
        }
        
        var rx = (pos.X - left) / imageWidth;
        var ry = (pos.Y - top) / imageHeight;
        
        if (rx < 0) rx = 0;
        if (rx > 1) rx = 1;
        
        if (ry < 0) ry = 0;
        if (ry > 1) ry = 1;

        _currentAnswer = $"{Math.Round(rx, 2).ToString(CultureInfo.InvariantCulture)},{Math.Round(ry, 2).ToString(CultureInfo.InvariantCulture)}";
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_bitmap == null) return;
        
        var parts = _currentAnswer.Split(',');
        if (parts.Length != 2 || !double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var rx) || !double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var ry))
        {
            SelectionMarker.Visibility = Visibility.Hidden;
            ErrorCircle.Visibility = Visibility.Hidden;
            return;
        }
        
        SelectionMarker.Visibility = Visibility.Visible;
        ErrorCircle.Visibility = Visibility.Visible;
        
        var scaleX = ReferenceGrid.ActualWidth / _bitmap.PixelWidth;
        var scaleY = ReferenceGrid.ActualHeight / _bitmap.PixelHeight;
        
        double imageWidth, imageHeight, left, top;
        
        if (scaleX < scaleY)
        {
            imageWidth = ReferenceGrid.ActualWidth;
            imageHeight = _bitmap.PixelHeight * scaleX;
            left = 0;
            top = (ReferenceGrid.ActualHeight - imageHeight) / 2;
        }
        else
        {
            imageWidth = _bitmap.PixelWidth * scaleY;
            imageHeight = ReferenceGrid.ActualHeight;
            left = (ReferenceGrid.ActualWidth - imageWidth) / 2;
            top = 0;
        }

        var px = left + rx * imageWidth;
        var py = top + ry * imageHeight;
        
        Canvas.SetLeft(SelectionMarker, px - SelectionMarker.Width / 2);
        Canvas.SetTop(SelectionMarker, py - SelectionMarker.Height / 2);
        
        var radX = CurrentDeviation * imageWidth;
        var radY = CurrentDeviation * imageHeight;
        
        ErrorCircle.Width = radX * 2;
        ErrorCircle.Height = radY * 2;
        Canvas.SetLeft(ErrorCircle, px - radX);
        Canvas.SetTop(ErrorCircle, py - radY);
    }
    
    private void ReferenceGrid_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateVisuals();
    
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Answer = _currentAnswer;
        _viewModel.Deviation = Math.Round(CurrentDeviation, 2);
        DialogResult = true;
    }
}
