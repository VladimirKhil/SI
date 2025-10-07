﻿using SIPackages;
using SIPackages.Core;
using SIPackages.Models;
using SIQuester.Contracts;
using SIQuester.Helpers;
using SIQuester.Implementation;
using SIQuester.Model;
using SIQuester.ViewModel;
using SIQuester.ViewModel.PlatformSpecific;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace SIQuester;

/// <summary>
/// Provides interaction logic for FlatDocView.xaml.
/// </summary>
public partial class FlatDocView : UserControl
{
    private bool _isDragging = false;
    private Point _startPoint;
    private Tuple<ThemeViewModel, int>? _insertionPosition;

    private readonly object _dragLock = new();

    public FlatDocView()
    {
        InitializeComponent();

        AppSettings.Default.PropertyChanged += Default_PropertyChanged; // TODO: fix memory leak
    }

    private void Default_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.Edit))
        {
            if (AppSettings.Default.Edit == EditMode.FloatPanel)
            {
                popup.IsOpen = true;
            }
            else
            {
                popup.IsOpen = false;
            }
        }
    }

    private void Main_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is TextBox textBox && textBox.IsFocused)
        {
            return;
        }

        var button = VisualHelper.TryFindAncestor<Button>((DependencyObject)e.OriginalSource);

        if (button != null)
        {
            return;
        }

        var host = VisualHelper.TryFindAncestor<Border>((DependencyObject)e.OriginalSource);

        if (host != null)
        {
            try
            {
                OnHostClick(host);
            }
            catch (Exception ex)
            {
                PlatformManager.Instance.ShowErrorMessage(ex.Message);
            }
        }

        if (AppSettings.Default.Edit != EditMode.FloatPanel)
        {
            _startPoint = e.GetPosition(null);
            PreviewMouseMove += MainWindow_PreviewMouseMove;
        }
    }

    private void OnHostClick(FrameworkElement host)
    {
        IItemViewModel itemViewModel;
        bool showEditor;

        switch (host.DataContext)
        {
            case QuestionViewModel question:
                itemViewModel = question;
                showEditor = question.Model.Price != Question.InvalidPrice;
                break;

            case ContentItemsViewModel contentItems:
                itemViewModel = contentItems.Owner;
                showEditor = contentItems.Owner.Model.Price != Question.InvalidPrice;
                break;

            case ThemeViewModel theme:
                itemViewModel = theme;
                showEditor = true;
                break;

            case RoundViewModel round:
                itemViewModel = round;
                showEditor = true;
                break;

            case PackageViewModel package:
                itemViewModel = package;
                showEditor = true;
                break;

            default:
                return;
        }

        var doc = (QDocument)DataContext;
        doc.Navigate.Execute(itemViewModel);

        popup.PlacementTarget = host;

        if (AppSettings.Default.Edit != EditMode.FloatPanel)
        {
            return;
        }

        popup.IsOpen = false;
        popup.IsOpen = true;

        var presenter = VisualTreeHelper.GetChild(_directEditHost, 0);
        var childrenCount = VisualTreeHelper.GetChildrenCount(presenter);

        if (childrenCount == 0)
        {
            return;
        }

        var childPresenter = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(presenter, 0), 0);
        var childrenCount2 = VisualTreeHelper.GetChildrenCount(childPresenter);

        if (showEditor && childrenCount2 == 0)
        {
            Dispatcher.BeginInvoke(CheckAndActivateEditor, host, childPresenter);
            return;
        }

        if (!showEditor)
        {
            var directEdit = (TextBoxBase)VisualTreeHelper.GetChild(childPresenter, 0);
            directEdit.Visibility = Visibility.Collapsed;
            return;
        }

        ActivateEditor(host, childPresenter);
    }

    private async void CheckAndActivateEditor(FrameworkElement host, DependencyObject childPresenter)
    {
        await Task.Delay(600);

        try
        {
            var childrenCount2 = VisualTreeHelper.GetChildrenCount(childPresenter);

            if (childrenCount2 == 0)
            {
                return;
            }

            ActivateEditor(host, childPresenter);
        }
        catch
        {
            // No op
        }
    }

    private void ActivateEditor(FrameworkElement host, DependencyObject childPresenter)
    {
        var directEdit = (TextBoxBase)VisualTreeHelper.GetChild(childPresenter, 0);

        var margin = host.TranslatePoint(new Point(0, 0), this);
        directEdit.Visibility = Visibility.Visible;
        directEdit.Margin = new Thickness(margin.X, margin.Y, 0, 0);

        Dispatcher.BeginInvoke(() =>
        {
            directEdit.Focus();
            directEdit.SelectAll();
            Keyboard.Focus(directEdit);
        });
    }

    private void DocumentView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        PreviewMouseMove -= MainWindow_PreviewMouseMove;
    }

    private void MainWindow_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        FrameworkElement? host;

        if (AppSettings.Default.FlatScale != FlatScale.Theme)
        {
            return;
        }

        lock (_dragLock)
        {
            if (_isDragging || _startPoint.X == -1)
            {
                return;
            }

            var position = e.GetPosition(null);

            if (Math.Abs(position.X - _startPoint.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(position.Y - _startPoint.Y) <= SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (AppSettings.Default.View == ViewMode.TreeFull)
            {
                host = VisualHelper.TryFindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            }
            else
            {
                host = VisualHelper.TryFindAncestor<Border>((DependencyObject)e.OriginalSource);
            }

            if (host == null || host.DataContext == null || host.DataContext is PackageViewModel || host.DataContext is not IItemViewModel)
            {
                return;
            }

            if (AppSettings.Default.View == ViewMode.Flat && (host.DataContext is RoundViewModel || host.DataContext is ThemeViewModel))
            {
                return;
            }

            if (DataContext == null)
            {
                return;
            }

            _isDragging = true;
        }

        var active = (QDocument)DataContext;
        var item = ((IItemViewModel)host.DataContext).GetModel();
        InfoOwnerData itemData;

        try
        {
            itemData = new InfoOwnerData(active, (IItemViewModel)host.DataContext);

            DragManager.DoDrag(
                host,
                active,
                itemData,
                () =>
                {
                    var rtb = new RenderTargetBitmap((int)host.ActualWidth, (int)host.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(host);
                    host.Opacity = 0.5;

                    dragImage.Width = host.ActualWidth;
                    dragImage.Height = host.ActualHeight;
                    dragImage.Source = rtb;
                    dragImage.Visibility = Visibility.Visible;
                },
                () =>
                {
                    dragImage.Source = null;
                });
        }
        catch (OutOfMemoryException)
        {
            MessageBox.Show(Properties.Resources.OOMCopyError, App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        catch (InvalidOperationException)
        {

        }
        finally
        {
            lock (_dragLock)
            {
                _isDragging = false;
            }

            e.Handled = true;

            PreviewMouseMove -= MainWindow_PreviewMouseMove;
        }
    }        

    private void Main_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = (e.Data.GetDataPresent(WellKnownDragFormats.FileName)
            || e.Data.GetDataPresent(WellKnownDragFormats.FileContents)
            || e.Data.GetDataPresent(typeof(InfoOwnerData))) ? e.AllowedEffects : DragDropEffects.None;

        e.Handled = true;
    }

    private void Main_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(InfoOwnerData)))
        {
            var pointer = e.GetPosition(grid);
            dragImage.Margin = new Thickness(pointer.X + 5, pointer.Y + 5, 0, 0);

            if (e.OriginalSource == line)
            {
                e.Effects = e.AllowedEffects;
                return;
            }

            var panel = VisualHelper.TryFindAncestor<StackPanel>((DependencyObject)e.OriginalSource);

            if (panel != null && AppSettings.Default.FlatLayoutMode == FlatLayoutMode.Table)
            {
                panel = VisualHelper.TryFindAncestor<StackPanel>(VisualTreeHelper.GetParent(panel));
            }

            if (panel == null)
            {
                line.Visibility = Visibility.Hidden;
                _insertionPosition = null;
                e.Effects = DragDropEffects.None;
                return;
            }

            ScrollHelper.ScrollView(e, scroller);

            // Show possible insertion point
            if (panel.DataContext is not ThemeViewModel themeViewModel)
            {
                line.Visibility = Visibility.Hidden;
                _insertionPosition = null;
                e.Effects = DragDropEffects.None;
                return;
            }

            var questionsCount = themeViewModel.Questions.Count;
            var rowsCount = AppSettings.Default.FlatLayoutMode == FlatLayoutMode.Table
                ? 1
                : (int)Math.Ceiling((double)questionsCount / 5);

            var lastRowCount = questionsCount - (rowsCount - 1) * 5;

            var borders = new int[Math.Min(5, questionsCount)];

            switch (questionsCount)
            {
                case 0:
                    break;

                case 1:
                    borders[0] = 120;
                    break;

                case 2:
                    borders[0] = 97;
                    borders[1] = 143;
                    break;

                case 3:
                    borders[0] = 74;
                    borders[1] = 120;
                    borders[2] = 166;
                    break;

                case 4:
                    borders[0] = 51;
                    borders[1] = 97;
                    borders[2] = 143;
                    borders[3] = 189;
                    break;

                default:
                    borders[0] = 28;
                    borders[1] = 74;
                    borders[2] = 120;
                    borders[3] = 166;
                    borders[4] = 212;
                    break;
            }

            var pos = e.GetPosition(panel);

            int rowIndex;

            if (AppSettings.Default.FlatLayoutMode == FlatLayoutMode.Table)
            {
                rowIndex = (int)Math.Floor(((double)pos.Y - 1) / 30);
            }
            else
            {
                // margins
                if (pos.Y >= 23 && pos.Y < 26)
                {
                    pos.Y = 26;
                }
                else if (pos.Y >= 26 + rowsCount * 30 && pos.Y < panel.ActualHeight - 6)
                {
                    pos.Y = 25 + rowsCount * 30 + (rowsCount > 0 ? 0 : 1);
                }

                rowIndex = (int)Math.Floor(((double)pos.Y - 26) / 30);
            }

            if (rowIndex < 0 || rowsCount > 0 && rowIndex >= rowsCount || rowsCount == 0 && rowIndex > 0)
            {
                line.Visibility = Visibility.Hidden;
                _insertionPosition = null;
                e.Effects = DragDropEffects.None;
                return;
            }

            double x = 0;
            double y = AppSettings.Default.FlatLayoutMode == FlatLayoutMode.Table ? 2 : 29 + rowIndex * 30;

            int index = 0;

            if (AppSettings.Default.FlatLayoutMode == FlatLayoutMode.Table)
            {
                index = (int)Math.Floor((pos.X - 272) / 43);
                x = 272 + index * 43;
            }
            else
            {
                var max = rowsCount > 0 ? (rowIndex < rowsCount - 1 ? borders.Length : lastRowCount) : 0;

                for (; index < max; index++)
                {
                    if (pos.X < borders[index])
                    {
                        x = borders[index] - 23;
                        break;
                    }
                }

                if (index == max)
                {
                    x = rowsCount > 0 ? borders[max - 1] + 23 : 120;
                }
            }

            _insertionPosition = Tuple.Create(themeViewModel, rowIndex * 5 + index);

            var delta = panel.TranslatePoint(new Point(x, y), grid);

            line.Visibility = Visibility.Visible;
            line.Margin = new Thickness(delta.X, delta.Y, 0, 0);
        }
        else
        {
            e.Effects = e.AllowedEffects;
        }

        e.Handled = true;
    }

    private void Main_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        try
        {
            line.Visibility = Visibility.Hidden;

            if (e.Data.GetDataPresent(WellKnownDragFormats.FileName))
            {
                if (e.Data.GetData(WellKnownDragFormats.FileName) is not string[] files || files.Length == 0)
                {
                    e.Effects = DragDropEffects.None;
                    return;
                }

                foreach (var file in files)
                {
                    var longPathString = FileHelper.GetLongPathName(file);
                    var fileExtension = Path.GetExtension(longPathString).ToLowerInvariant();

                    switch (fileExtension)
                    {
                        case ".txt":
                            ((MainViewModel)Application.Current.MainWindow.DataContext).ImportTxt.Execute(longPathString);
                            break;

                        case ".siq":
                            ApplicationCommands.Open.Execute(longPathString, this);
                            break;

                        default:
                            foreach (var item in Quality.FileExtensions)
                            {
                                if (item.Value.Contains(fileExtension))
                                {
                                    TryImportMedia(e, longPathString, item.Key);
                                    break;
                                }
                            }
                            break;
                    }
                }

                e.Effects = e.AllowedEffects;
                return;
            }

            if (e.Data.GetDataPresent(WellKnownDragFormats.FileContents))
            {
                using (var contentStream = e.Data.GetData(WellKnownDragFormats.FileContents) as MemoryStream)
                {
                    ((MainViewModel)Application.Current.MainWindow.DataContext).ImportTxt.Execute(contentStream);
                }

                e.Effects = e.AllowedEffects;
                return;
            }

            var format = WellKnownDragFormats.GetDragFormat(e);
            var document = (QDocument)DataContext;

            InfoOwnerData dragData;

            try
            {
                dragData = (InfoOwnerData)e.Data.GetData(typeof(InfoOwnerData));
            }
            catch (SerializationException)
            {
                // TODO: log
                return;
            }

            e.Effects = DragDropEffects.Move;

            if (format == WellKnownDragFormats.Question && _insertionPosition != null)
            {
                Question question;

                if (dragData != null)
                {
                    question = (Question)dragData.GetItem();
                }
                else
                {
                    var value = e.Data.GetData(DataFormats.Serializable).ToString();
                    using var stringReader = new StringReader(value);
                    using var reader = XmlReader.Create(stringReader);

                    question = new Question();
                    question.ReadXml(reader);
                }

                var themeViewModel = _insertionPosition.Item1;
                var index = _insertionPosition.Item2;

                if (themeViewModel.Questions.Any(questionViewModel => questionViewModel.Model == question))
                {
                    question = question.Clone();
                }

                var currentPrices = themeViewModel.CapturePrices();

                var questionViewModelNew = new QuestionViewModel(question);
                themeViewModel.Questions.Insert(index, questionViewModelNew);

                if (AppSettings.Default.ChangePriceOnMove)
                {
                    themeViewModel.ResetPrices(currentPrices);
                }

                document.ApplyData(dragData);
                document.Navigate.Execute(questionViewModelNew);
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Main_Drop error: {ex}");
        }
    }

    private void TryImportMedia(DragEventArgs e, string filePath, string mediaType)
    {
        var contentType = CollectionNames.TryGetContentType(mediaType);

        if (contentType == null)
        {
            return;
        }

        var itemsControl = VisualHelper.TryFindAncestor<ItemsControl>((DependencyObject)e.OriginalSource);

        if (itemsControl == null)
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        if (itemsControl.DataContext is not ContentItemsViewModel contentItemsViewModel)
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        var document = (QDocument)DataContext;

        var collection = document.GetCollection(mediaType);
        var item = collection.AddFile(filePath);

        if (contentItemsViewModel.Count > 0 && string.IsNullOrWhiteSpace(contentItemsViewModel[^1].Model.Value))
        {
            contentItemsViewModel.RemoveAt(contentItemsViewModel.Count - 1);
        }

        contentItemsViewModel.Add(new ContentItemViewModel(new ContentItem
        { 
            Type = contentType,
            IsRef = true,
            Value = item.Model.Name,
            Placement = contentType == ContentTypes.Audio ? ContentPlacements.Background : ContentPlacements.Screen
        }));

        if (AppSettings.Default.SetRightAnswerFromFileName)
        {
            contentItemsViewModel.Owner.TryAddRightAnswerFromFileName(item.Model.Name);
        }
    }

    internal static void RecountPrices(Theme theme, int pos, bool down)
    {
        int total = theme.Questions.Count;

        if (down)
        {
            for (int i = pos; i < total - 1; i++)
            {
                theme.Questions[i].Price = theme.Questions[i + 1].Price;
            }

            if (total == 2)
            {
                theme.Questions[total - 1].Price = theme.Questions[0].Price * 2;
            }
            else if (total > 2)
            {
                theme.Questions[total - 1].Price = 2 * theme.Questions[total - 2].Price - theme.Questions[total - 3].Price;
            }
        }
        else
        {
            for (int i = total - 1; i > pos; i--)
            {
                theme.Questions[i].Price = theme.Questions[i - 1].Price;
            }
        }
    }
}
