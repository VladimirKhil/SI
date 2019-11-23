using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace SIQuester
{
    /// <summary>
    /// Interaction logic for FlatDocView.xaml
    /// </summary>
    public partial class FlatDocView : UserControl
    {
        private bool _isDragging = false;
        private Point _startPoint;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int GetLongPathName([MarshalAs(UnmanagedType.LPWStr)] string lpszShortPath, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpszLongPath, [MarshalAs(UnmanagedType.U4)] int cchBuffer);

        private Tuple<ThemeViewModel, int> _insertionPosition;

        private readonly object _dragLock = new object();

        public FlatDocView()
        {
            InitializeComponent();

            AppSettings.Default.PropertyChanged += Default_PropertyChanged;
        }

        void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.Edit))
            {
                if (AppSettings.Default.Edit == EditMode.FloatPanel)
                    popup.IsOpen = true;
                else
                    popup.IsOpen = false;
            }
        }

        private void Main_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox && textBox.IsFocused)
                return;

            var host = FindAncestor<Border>((DependencyObject)e.OriginalSource);
            if (host != null)
            {
                if (host.DataContext is QuestionViewModel question)
                {
                    var doc = (QDocument)DataContext;
                    doc.Navigate.Execute(question);

                    popup.PlacementTarget = host;
                    if (AppSettings.Default.Edit == EditMode.FloatPanel)
                    {
                        popup.IsOpen = false;
                        popup.IsOpen = true;

                        var margin = host.TranslatePoint(new Point(0, 0), this);
                        directEdit.Visibility = Visibility.Visible;
                        directEdit.Margin = new Thickness(margin.X, margin.Y, 0, 0);
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            directEdit.Focus();
                            directEdit.SelectAll();
                            Keyboard.Focus(directEdit);
                        }));
                    }
                }
            }

            if (AppSettings.Default.Edit != EditMode.FloatPanel)
            {
                _startPoint = e.GetPosition(null);
                PreviewMouseMove += MainWindow_PreviewMouseMove;
            }
        }

        void DocumentView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PreviewMouseMove -= MainWindow_PreviewMouseMove;
        }

        void MainWindow_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement host;

            if (AppSettings.Default.FlatScale != FlatScale.Theme)
                return;

            lock (_dragLock)
            {
                if (_isDragging || _startPoint.X == -1)
                    return;

                var position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) <= 5 * SystemParameters.MinimumHorizontalDragDistance &&
                    Math.Abs(position.Y - _startPoint.Y) <= 5 * SystemParameters.MinimumVerticalDragDistance)
                    return;

                if (AppSettings.Default.View == ViewMode.TreeFull)
                    host = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                else
                    host = FindAncestor<Border>((DependencyObject)e.OriginalSource);

                if (host == null || host.DataContext == null || host.DataContext is PackageViewModel || !(host.DataContext is IItemViewModel))
                    return;

                if (AppSettings.Default.View == ViewMode.Flat && (host.DataContext is RoundViewModel || host.DataContext is ThemeViewModel))
                    return;

                if (DataContext == null)
                    return;

                _isDragging = true;
            }

            var active = (QDocument)DataContext;
            var item = ((IItemViewModel)host.DataContext).GetModel();
            InfoOwnerData itemData = null;

            try
            {
                itemData = new InfoOwnerData(item);
                DoDrag(host, active, item, itemData, () =>
                    {
                        var rtb = new RenderTargetBitmap((int)host.ActualWidth, (int)host.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(host);
                        host.Opacity = 0.5;

                        dragImage.Width = host.ActualWidth;
                        dragImage.Height = host.ActualHeight;
                        dragImage.Source = rtb;
                        dragImage.Visibility = Visibility.Visible;
                    }, () =>
                    {
                        dragImage.Source = null;
                    });
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("Ошибка копирования данных: слишком большой объём", App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

                if (itemData != null)
                    itemData.Dispose();
            }
        }

        internal static void DoDrag(FrameworkElement host, QDocument active, InfoOwner item, InfoOwnerData itemData, Action beforeDrag = null, Action afterDrag = null)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            if (active == null)
                throw new ArgumentNullException(nameof(active));

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (itemData == null)
                throw new ArgumentNullException(nameof(itemData));

            itemData.GetFullData(active.Document, item);
            var dataObject = new DataObject(itemData);

            if (host.DataContext is RoundViewModel roundViewModel)
            {
                var packageViewModel = roundViewModel.OwnerPackage;
                if (packageViewModel == null)
                    throw new ArgumentException(nameof(packageViewModel));

                int index = packageViewModel.Rounds.IndexOf(roundViewModel);

                active.BeginChange();

                try
                {
                    var sb = new StringBuilder();
                    using (var writer = XmlWriter.Create(sb))
                    {
                        roundViewModel.Model.WriteXml(writer);
                    }

                    dataObject.SetData("siqround", "1");
                    dataObject.SetData(DataFormats.Serializable, sb);

                    var result = DragDrop.DoDragDrop(host, dataObject, DragDropEffects.Move);
                    if (result == DragDropEffects.Move)
                    {
                        if (packageViewModel.Rounds[index] != roundViewModel)
                            index++;

                        packageViewModel.Rounds.RemoveAt(index);
                        active.CommitChange();
                    }
                    else
                        active.RollbackChange();
                }
                catch (Exception exc)
                {
                    active.RollbackChange();
                    throw exc;
                }
            }
            else
            {
                if (host.DataContext is ThemeViewModel themeViewModel)
                {
                    roundViewModel = themeViewModel.OwnerRound;
                    if (roundViewModel == null)
                        throw new ArgumentException(nameof(roundViewModel));

                    int index = roundViewModel.Themes.IndexOf(themeViewModel);

                    active.BeginChange();

                    try
                    {
                        var sb = new StringBuilder();
                        using (var writer = XmlWriter.Create(sb))
                        {
                            themeViewModel.Model.WriteXml(writer);
                        }

                        dataObject.SetData("siqtheme", "1");
                        dataObject.SetData(DataFormats.Serializable, sb);

                        var result = DragDrop.DoDragDrop(host, dataObject, DragDropEffects.Move);
                        if (result == DragDropEffects.Move)
                        {
                            if (roundViewModel.Themes[index] != themeViewModel)
                                index++;

                            roundViewModel.Themes.RemoveAt(index);
                        }

                        active.CommitChange();
                    }
                    catch (Exception exc)
                    {
                        active.RollbackChange();
                        throw exc;
                    }
                }
                else
                {
                    var questionViewModel = host.DataContext as QuestionViewModel;
                    themeViewModel = questionViewModel.OwnerTheme;
                    if (themeViewModel == null)
                        throw new ArgumentException(nameof(themeViewModel));

                    var index = themeViewModel.Questions.IndexOf(questionViewModel);
                    active.BeginChange();

                    try
                    {
                        var sb = new StringBuilder();
                        using (var writer = XmlWriter.Create(sb))
                        {
                            questionViewModel.Model.WriteXml(writer);
                        }

                        dataObject.SetData("siqquestion", "1");
                        dataObject.SetData(DataFormats.Serializable, sb);

                        beforeDrag?.Invoke();

                        DragDropEffects result;
                        try
                        {
                            result = DragDrop.DoDragDrop(host, dataObject, DragDropEffects.Move);
                        }
                        catch (InvalidOperationException)
                        {
                            result = DragDropEffects.None;
                        }
                        finally
                        {
                            host.Opacity = 1.0;

                            afterDrag?.Invoke();
                        }

                        if (result == DragDropEffects.Move)
                        {
                            if (themeViewModel.Questions[index] != questionViewModel)
                                index++;

                            themeViewModel.Questions.RemoveAt(index);

                            if (AppSettings.Default.ChangePriceOnMove)
                                RecountPrices(themeViewModel);
                        }

                        active.CommitChange();
                    }
                    catch (Exception exc)
                    {
                        active.RollbackChange();
                        throw exc;
                    }
                }
            }
        }

        private void Main_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = (e.Data.GetDataPresent("FileName")
                || e.Data.GetDataPresent("FileContents")
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

                var panel = FindAncestor<StackPanel>((DependencyObject)e.OriginalSource);
                if (panel == null)
                {
                    line.Visibility = Visibility.Hidden;
                    _insertionPosition = null;
                    e.Effects = DragDropEffects.None;
                    return;
                }

                ScrollView(e, scroller);

                // Покажем предполагаемое место для вставки
                if (!(panel.DataContext is ThemeViewModel themeViewModel))
                {
                    line.Visibility = Visibility.Hidden;
                    _insertionPosition = null;
                    e.Effects = DragDropEffects.None;
                    return;
                }

                var questionsCount = themeViewModel.Questions.Count;
                var rowsCount = (int)Math.Ceiling((double)questionsCount / 5);
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
                double x = 0;

                // поля
                if (pos.Y >= 23 && pos.Y < 26)
                    pos.Y = 26;
                else if (pos.Y >= 26 + rowsCount * 30 && pos.Y < panel.ActualHeight - 6)
                    pos.Y = 25 + rowsCount * 30 + (rowsCount > 0 ? 0 : 1);

                var row = (int)Math.Floor(((double)pos.Y - 26) / 30);

                if (row < 0 || rowsCount > 0 && row >= rowsCount || rowsCount == 0 && row > 0)
                {
                    line.Visibility = Visibility.Hidden;
                    _insertionPosition = null;
                    e.Effects = DragDropEffects.None;
                    return;
                }

                double y = 29 + row * 30;

                int index = 0;
                var max = rowsCount > 0 ? (row < rowsCount - 1 ? borders.Length : lastRowCount) : 0;
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

                _insertionPosition = Tuple.Create(themeViewModel, row * 5 + index);

                line.Visibility = Visibility.Visible;
                var delta = panel.TranslatePoint(new Point(x, y), grid);
                line.Margin = new Thickness(delta.X, delta.Y, 0, 0);
            }
            else
            {
                e.Effects = e.AllowedEffects;
                e.Handled = true;
            }
        }

        private async void Main_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            line.Visibility = Visibility.Hidden;

            if (e.Data.GetDataPresent("FileName"))
            {
                var files = e.Data.GetData("FileName") as string[];
                foreach (var file in files)
                {
                    var longPath = new StringBuilder(255);
                    GetLongPathName(file, longPath, longPath.Capacity);

                    var longPathString = longPath.ToString();

                    if (Path.GetExtension(longPathString) == ".txt")
                    {
                        ((MainViewModel)Application.Current.MainWindow.DataContext).ImportTxt.Execute(longPathString);
                    }
                    else
                        ApplicationCommands.Open.Execute(longPathString, this);
                }

                e.Effects = e.AllowedEffects;
                return;
            }

            if (e.Data.GetDataPresent("FileContents"))
            {
                using (var contentStream = e.Data.GetData("FileContents") as MemoryStream)
                {
                    ((MainViewModel)Application.Current.MainWindow.DataContext).ImportTxt.Execute(contentStream);
                }

                e.Effects = e.AllowedEffects;
                return;
            }

            var format = GetDragFormat(e);

            InfoOwnerData dragData = null;
            try
            {
                dragData = (InfoOwnerData)e.Data.GetData(typeof(InfoOwnerData));
            }
            catch (SerializationException)
            {
                return;
            }

            e.Effects = DragDropEffects.Move;
            try
            {
                if (format == "siqquestion" && _insertionPosition != null)
                {
                    Question question = null;
                    if (dragData != null)
                    {
                        question = (Question)dragData.GetItem();
                    }
                    else
                    {
                        var value = e.Data.GetData(DataFormats.Serializable).ToString();
                        using (var stringReader = new StringReader(value))
                        using (var reader = XmlReader.Create(stringReader))
                        {
                            question = new Question();
                            question.ReadXml(reader);
                        }
                    }

                    var themeViewModel = _insertionPosition.Item1;
                    var index = _insertionPosition.Item2;

                    if (themeViewModel.Questions.Any(questionViewModel => questionViewModel.Model == question))
                        question = question.Clone();

                    var questionViewModelNew = new QuestionViewModel(question);
                    themeViewModel.Questions.Insert(index, questionViewModelNew);

                    if (AppSettings.Default.ChangePriceOnMove)
                        RecountPrices(themeViewModel);

                    var document = (QDocument)DataContext;
                    await dragData.ApplyData(document.Document);
                    document.Navigate.Execute(questionViewModelNew);
                }
                else
                    e.Effects = DragDropEffects.None;
            }
            finally
            {
                if (dragData != null)
                {
                    dragData.Dispose();
                }
            }
        }

        internal static void ScrollView(DragEventArgs e, ScrollViewer scroller)
        {
            var pos = e.GetPosition(scroller);

            double scrollOffset = 0.0;

            // See if we need to scroll down 
            if (scroller.ViewportHeight - pos.Y < 40.0)
            {
                scrollOffset = 6.0;
            }
            else if (pos.Y < 40.0)
            {
                scrollOffset = -6.0;
            }

            // Scroll the tree down or up 
            if (scrollOffset != 0.0)
            {
                scrollOffset += scroller.VerticalOffset;

                if (scrollOffset < 0.0)
                {
                    scrollOffset = 0.0;
                }
                else if (scrollOffset > scroller.ScrollableHeight)
                {
                    scrollOffset = scroller.ScrollableHeight;
                }

                scroller.ScrollToVerticalOffset(scrollOffset);
            }
        }

        internal static string GetDragFormat(DragEventArgs e)
        {
            return e.Data.GetFormats(false).FirstOrDefault(f => f.StartsWith("siq"));
        }

        internal static T FindAncestor<T>(DependencyObject descendant)
            where T : class
        {
            do
            {
                if (descendant is T item)
                    return item;

                if (!(descendant is Visual))
                    return null;

                descendant = VisualTreeHelper.GetParent(descendant);
            } while (descendant != null);
            return null;
        }

        internal static void RecountPrices(Theme theme, int pos, bool down)
        {
            int total = theme.Questions.Count;
            if (down)
            {
                for (int i = pos; i < total - 1; i++)
                    theme.Questions[i].Price = theme.Questions[i + 1].Price;

                if (total == 2)
                    theme.Questions[total - 1].Price = theme.Questions[0].Price * 2;
                else if (total > 2)
                    theme.Questions[total - 1].Price = 2 * theme.Questions[total - 2].Price - theme.Questions[total - 3].Price;
            }
            else
            {
                for (int i = total - 1; i > pos; i--)
                    theme.Questions[i].Price = theme.Questions[i - 1].Price;
            }
        }

        internal static void RecountPrices(ThemeViewModel theme)
        {
            var round = theme.OwnerRound;
            var coef = round.Model.Type == RoundTypes.Final ? 0 : round.OwnerPackage.Rounds.IndexOf(round) + 1;

            for (int i = 0; i < theme.Questions.Count; i++)
            {
                theme.Questions[i].Model.Price = coef * AppSettings.Default.QuestionBase * (i + 1);
            }
        }
    }
}
