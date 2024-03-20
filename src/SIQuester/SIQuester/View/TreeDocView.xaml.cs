using SIPackages;
using SIPackages.Core;
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
using System.Windows.Input;
using System.Xml;

namespace SIQuester;

/// <summary>
/// Provides interaction logic for TreeDocView.xaml.
/// </summary>
public partial class TreeDocView : UserControl
{
    private bool _isDragging = false;

    private Point _startPoint;
    
    private readonly object _dragLock = new();

    public TreeDocView() => InitializeComponent();

    private void Main_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is UIElement element && element.GetType().ToString() == "System.Windows.Controls.TextBoxView")
        {
            return;
        }

        _startPoint = e.GetPosition(null);

        PreviewMouseMove += MainWindow_PreviewMouseMove;
        PreviewMouseLeftButtonUp += DocumentView_PreviewMouseLeftButtonUp;
    }

    private void DocumentView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        PreviewMouseMove -= MainWindow_PreviewMouseMove;
        PreviewMouseLeftButtonUp -= DocumentView_PreviewMouseLeftButtonUp;
    }

    private void MainWindow_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        FrameworkElement host;

        lock (_dragLock)
        {
            if (_isDragging)
            {
                return;
            }

            var position = e.GetPosition(null);

            if (Math.Abs(position.X - _startPoint.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(position.Y - _startPoint.Y) <= SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            host = AppSettings.Default.View == ViewMode.TreeFull
                ? VisualHelper.TryFindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource)
                : VisualHelper.TryFindAncestor<Border>((DependencyObject)e.OriginalSource);

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
            DragManager.DoDrag(host, active, item, itemData);
        }
        catch (OutOfMemoryException)
        {
            MessageBox.Show(
                $"{Properties.Resources.DataCopyError}: {Properties.Resources.FileTooLarge}",
                App.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
        }
        catch (InvalidOperationException)
        {

        }
        catch (Exception exc)
        {
            MessageBox.Show(
                $"{Properties.Resources.DataCopyError}: {exc.Message}",
                App.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
        }
        finally
        {
            lock (_dragLock)
            {
                _isDragging = false;
            }

            e.Handled = true;

            PreviewMouseMove -= MainWindow_PreviewMouseMove;
            PreviewMouseLeftButtonUp -= DocumentView_PreviewMouseLeftButtonUp;
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
            ScrollHelper.ScrollView(e);
            SetEffect(e);
        }
        else
        {
            e.Effects = e.AllowedEffects;
            e.Handled = true;
        }
    }

    internal void SetEffect(DragEventArgs e)
    {
        var treeViewItem = VisualHelper.TryFindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

        if (treeViewItem == null)
        {
            e.Effects = DragDropEffects.None;
            _insertionMark.Visibility = Visibility.Hidden;
        }
        else
        {
            var format = WellKnownDragFormats.GetDragFormat(e);

            e.Effects =
                (treeViewItem.DataContext is PackageViewModel || treeViewItem.DataContext is RoundViewModel) &&
                    format == WellKnownDragFormats.Round ||
                (treeViewItem.DataContext is RoundViewModel || treeViewItem.DataContext is ThemeViewModel) &&
                    format == WellKnownDragFormats.Theme ||
                (treeViewItem.DataContext is ThemeViewModel || treeViewItem.DataContext is QuestionViewModel) &&
                    format == WellKnownDragFormats.Question
                ? DragDropEffects.Move
                : DragDropEffects.None;

            if (e.Effects == DragDropEffects.Move)
            {
                var isDroppingToParent = 
                    treeViewItem.DataContext is PackageViewModel && format == WellKnownDragFormats.Round ||
                    treeViewItem.DataContext is RoundViewModel && format == WellKnownDragFormats.Theme ||
                    treeViewItem.DataContext is ThemeViewModel && format == WellKnownDragFormats.Question;

                var treeViewItemPosition = treeViewItem.TranslatePoint(new Point(), _grid);

                _insertionMark.Visibility = Visibility.Visible;
                _insertionMark.Margin = new Thickness(0, treeViewItemPosition.Y + (isDroppingToParent ? treeViewItem.ActualHeight : 0), 0, 0);
            }
            else
            {
                _insertionMark.Visibility = Visibility.Hidden;
            }
        }

        e.Handled = true;
    }

    private void Main_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        try
        {
            _insertionMark.Visibility = Visibility.Hidden;

            if (e.Data.GetDataPresent(WellKnownDragFormats.FileName))
            {
                if (e.Data.GetData(WellKnownDragFormats.FileName) is not string[] files)
                {
                    return;
                }

                foreach (var file in files)
                {
                    var longPathString = FileHelper.GetLongPathName(file);
                    var fileExtension = Path.GetExtension(longPathString);

                    switch (fileExtension)
                    {
                        case ".txt":
                            ((MainViewModel)Application.Current.MainWindow.DataContext).ImportTxt.Execute(longPathString);
                            break;

                        case ".siq":
                            ApplicationCommands.Open.Execute(longPathString, this);
                            break;

                        default:
                            foreach (var item in MediaOwnerViewModel.RecommenedExtensions)
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

            var treeViewItem = VisualHelper.TryFindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

            if (treeViewItem == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            var format = WellKnownDragFormats.GetDragFormat(e);

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

            var dataContext = ((IItemViewModel)treeViewItem.DataContext).GetModel();

            if (dataContext == null || dataContext.GetType().ToString() == format && Equals(dataContext, dragData.ItemData))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            var document = (QDocument)DataContext;

            e.Effects = DragDropEffects.Move;

            if (format == WellKnownDragFormats.Round)
            {
                Round round;

                if (dragData != null)
                {
                    round = (Round)dragData.GetItem();
                }
                else
                {
                    var value = e.Data.GetData(DataFormats.Serializable).ToString();

                    using var stringReader = new StringReader(value);
                    using var reader = XmlReader.Create(stringReader);
                    round = new Round();
                    round.ReadXml(reader);
                }

                // Remove later
                foreach (var theme in round.Themes)
                {
                    foreach (var question in theme.Questions)
                    {
                        question.Upgrade();
                    }
                }

                if (treeViewItem.DataContext is PackageViewModel package)
                {
                    package.Rounds.Add(new RoundViewModel(round) { IsSelected = true });
                    document.ApplyData(dragData);

                    package.IsExpanded = true;
                }
                else if (treeViewItem.DataContext is RoundViewModel)
                {
                    var docRound = treeViewItem.DataContext as RoundViewModel;

                    docRound.OwnerPackage.Rounds.Insert(
                        docRound.OwnerPackage.Rounds.IndexOf(docRound),
                        new RoundViewModel(round) { IsSelected = true });

                    document.ApplyData(dragData);
                }
            }
            else if (format == WellKnownDragFormats.Theme)
            {
                Theme theme;

                if (dragData != null)
                {
                    theme = (Theme)dragData.GetItem();
                }
                else
                {
                    var value = e.Data.GetData(DataFormats.Serializable).ToString();
                    using var stringReader = new StringReader(value);
                    using var reader = XmlReader.Create(stringReader);
                    theme = new Theme();
                    theme.ReadXml(reader);
                }

                // Remove later
                foreach (var question in theme.Questions)
                {
                    question.Upgrade();
                }

                if (treeViewItem.DataContext is RoundViewModel docRound)
                {
                    docRound.Themes.Add(new ThemeViewModel(theme) { IsSelected = true });
                    document.ApplyData(dragData);

                    docRound.IsExpanded = true;
                }
                else if (treeViewItem.DataContext is ThemeViewModel docTheme)
                {
                    docTheme.OwnerRound.Themes.Insert(
                        docTheme.OwnerRound.Themes.IndexOf(docTheme),
                        new ThemeViewModel(theme) { IsSelected = true });

                    document.ApplyData(dragData);
                }
            }
            else if (format == WellKnownDragFormats.Question)
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

                // Remove later
                question.Upgrade();

                if (treeViewItem.DataContext is ThemeViewModel targetTheme)
                {
                    if (targetTheme.Questions.Any(questionViewModel => questionViewModel.Model == question))
                    {
                        question = question.Clone(); // That enables original question safe remove after drag finish
                    }

                    var currentPrices = targetTheme.CapturePrices();

                    targetTheme.Questions.Add(new QuestionViewModel(question) { IsSelected = true });

                    if (AppSettings.Default.ChangePriceOnMove)
                    {
                        targetTheme.ResetPrices(currentPrices);
                    }

                    targetTheme.IsExpanded = true;

                    if (dragData != null)
                    {
                        document.ApplyData(dragData);
                    }
                }
                else if (treeViewItem.DataContext is QuestionViewModel targetQuestion)
                {
                    var targetOwnerTheme = targetQuestion.OwnerTheme;

                    if (targetQuestion.Model == question || targetOwnerTheme == null)
                    {
                        e.Effects = DragDropEffects.None;
                        return;
                    }

                    if (targetOwnerTheme.Questions.Any(questionViewModel => questionViewModel.Model == question))
                    {
                        question = question.Clone(); // That enables original question safe remove after drag finish
                    }

                    var currentPrices = targetOwnerTheme.CapturePrices();

                    var questionIndex = targetOwnerTheme.Questions.IndexOf(targetQuestion);
                    targetOwnerTheme.Questions.Insert(questionIndex, new QuestionViewModel(question) { IsSelected = true });

                    if (AppSettings.Default.ChangePriceOnMove)
                    {
                        targetOwnerTheme.ResetPrices(currentPrices);
                    }

                    if (dragData != null)
                    {
                        document.ApplyData(dragData);
                    }
                }
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
            var question = contentItemsViewModel.Owner;

            if (question.Right.Last().Length == 0)
            {
                question.Right.RemoveAt(question.Right.Count - 1);
            }

            question.Right.Add(Path.GetFileNameWithoutExtension(item.Model.Name));
        }
    }
}
