using SIPackages;
using SIQuester.Contracts;
using SIQuester.Implementation;
using SIQuester.Model;
using SIQuester.ViewModel;
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
                ? FlatDocView.FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource)
                : FlatDocView.FindAncestor<Border>((DependencyObject)e.OriginalSource);

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

        InfoOwnerData itemData = null;

        try
        {
            itemData = new InfoOwnerData(active, (IItemViewModel)host.DataContext);
            DragManager.DoDrag(host, active, item, itemData);
        }
        catch (OutOfMemoryException)
        {
            MessageBox.Show(
                "Ошибка копирования данных: слишком большой объём",
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
                string.Format("Ошибка копирования данных: {0}", exc.Message),
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
        var treeViewItem = FlatDocView.FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

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
                var files = e.Data.GetData(WellKnownDragFormats.FileName) as string[];

                foreach (var file in files)
                {
                    var longPathString = FileHelper.GetLongPathName(file);

                    if (Path.GetExtension(longPathString) == ".txt")
                    {
                        ((MainViewModel)Application.Current.MainWindow.DataContext).ImportTxt.Execute(longPathString);
                    }
                    else
                    {
                        ApplicationCommands.Open.Execute(longPathString, this);
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

            var treeViewItem = FlatDocView.FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

            if (treeViewItem == null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            var format = WellKnownDragFormats.GetDragFormat(e);

            InfoOwnerData dragData = null;

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
                Round round = null;

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

                if (treeViewItem.DataContext is PackageViewModel)
                {
                    var package = treeViewItem.DataContext as PackageViewModel;
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
                Theme theme = null;

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

                if (treeViewItem.DataContext is RoundViewModel)
                {
                    var docRound = treeViewItem.DataContext as RoundViewModel;
                    docRound.Themes.Add(new ThemeViewModel(theme) { IsSelected = true });
                    document.ApplyData(dragData);

                    docRound.IsExpanded = true;
                }
                else if (treeViewItem.DataContext is ThemeViewModel)
                {
                    var docTheme = treeViewItem.DataContext as ThemeViewModel;

                    docTheme.OwnerRound.Themes.Insert(
                        docTheme.OwnerRound.Themes.IndexOf(docTheme),
                        new ThemeViewModel(theme) { IsSelected = true });

                    document.ApplyData(dragData);
                }
            }
            else if (format == WellKnownDragFormats.Question)
            {
                Question question = null;

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

                if (treeViewItem.DataContext is ThemeViewModel)
                {
                    var docTheme = treeViewItem.DataContext as ThemeViewModel;

                    if (docTheme.Questions.Any(questionViewModel => questionViewModel.Model == question))
                    {
                        question = question.Clone();
                    }

                    docTheme.Questions.Add(new QuestionViewModel(question) { IsSelected = true });

                    if (AppSettings.Default.ChangePriceOnMove)
                    {
                        DragManager.RecountPrices(docTheme);
                    }

                    docTheme.IsExpanded = true;

                    document.ApplyData(dragData);
                }
                else if (treeViewItem.DataContext is QuestionViewModel)
                {
                    var docQuestion = treeViewItem.DataContext as QuestionViewModel;

                    if (AreEqual(docQuestion.Model, question))
                    {
                        e.Effects = DragDropEffects.None;
                        return;
                    }

                    if (docQuestion.OwnerTheme.Questions.Any(questionViewModel => questionViewModel.Model == question))
                    {
                        question = question.Clone();
                    }

                    int pos = docQuestion.OwnerTheme.Questions.IndexOf(docQuestion);
                    docQuestion.OwnerTheme.Questions.Insert(pos, new QuestionViewModel(question) { IsSelected = true });

                    if (AppSettings.Default.ChangePriceOnMove)
                    {
                        DragManager.RecountPrices(docQuestion.OwnerTheme);
                    }

                    document.ApplyData(dragData);
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

    private static bool AreEqual(Question question1, Question question2)
    {
        if (question1.Info.Authors.Count != question2.Info.Authors.Count)
        {
            return false;
        }

        for (int i = 0; i < question1.Info.Authors.Count; i++)
        {
            if (question1.Info.Authors[i] != question2.Info.Authors[i])
            {
                return false;
            }
        }

        if (question1.Info.Sources.Count != question2.Info.Sources.Count)
        {
            return false;
        }

        for (int i = 0; i < question1.Info.Sources.Count; i++)
        {
            if (question1.Info.Sources[i] != question2.Info.Sources[i])
            {
                return false;
            }
        }

        if (question1.Info.Comments.Text != question2.Info.Comments.Text)
        {
            return false;
        }

        if (question1.Info.Extension != question2.Info.Extension)
        {
            return false;
        }

        if (question1.Price != question2.Price)
        {
            return false;
        }

        if (question1.Type.Name != question2.Type.Name)
        {
            return false;
        }

        if (question1.Type.Params.Count != question2.Type.Params.Count)
        {
            return false;
        }

        for (int i = 0; i < question1.Type.Params.Count; i++)
        {
            if (question1.Type.Params[i].Name != question2.Type.Params[i].Name)
            {
                return false;
            }

            if (question1.Type.Params[i].Value != question2.Type.Params[i].Value)
            {
                return false;
            }
        }

        if (!Equals(question1.Parameters, question2.Parameters))
        {
            return false;
        }

        if (!Equals(question1.Script, question2.Script))
        {
            return false;
        }

        var scenario1 = question1.Scenario;
        var scenario2 = question2.Scenario;

        if (scenario1.Count != scenario2.Count)
        {
            return false;
        }

        for (int i = 0; i < scenario1.Count; i++)
        {
            if (scenario1[i].Type != scenario2[i].Type)
            {
                return false;
            }

            if (scenario1[i].Text != scenario2[i].Text)
            {
                return false;
            }

            if (scenario1[i].AtomTime != scenario2[i].AtomTime)
            {
                return false;
            }
        }

        if (question1.Right.Count != question2.Right.Count)
        {
            return false;
        }

        for (int i = 0; i < question1.Right.Count; i++)
        {
            if (question1.Right[i] != question2.Right[i])
            {
                return false;
            }
        }

        if (question1.Wrong.Count != question2.Wrong.Count)
        {
            return false;
        }

        for (int i = 0; i < question1.Wrong.Count; i++)
        {
            if (question1.Wrong[i] != question2.Wrong[i])
            {
                return false;
            }
        }

        return true;
    }
}
