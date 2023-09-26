using MahApps.Metro.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAPICodePack.Taskbar;
using SIQuester.ViewModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Utils;

namespace SIQuester;

/// <summary>
/// Defines main application window.
/// </summary>
public partial class MainWindow : MetroWindow
{
    private bool _closingFromThumbnail = false;

    public MainWindow() => InitializeComponent();

    /// <summary>
    /// Performs manual binding between DocList collection and tabs. TODO: switch to automatic binding in the future.
    /// </summary>
    private void DocList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                var tabContent = new ContentControl
                {
                    Content = e.NewItems[0],
                    ContentTemplateSelector = (DataTemplateSelector)Application.Current.Resources["ContentSelector"]
                };

                var tabItem = new TabItem
                {
                    Header = new ContentControl { Content = e.NewItems[0], ContentTemplate = (DataTemplate)Resources["TabItemHeaderTemplate"] },
                    Content = tabContent,
                    DataContext = e.NewItems[0],
                    Padding = new Thickness(3,1,1,1),
                    BorderThickness = new Thickness(10)
                };

                tabItem.DragEnter += TabItem_DragEnter;
                tabItem.DragOver += TabItem_DragOver;
                tabItem.Loaded += TabItem_Loaded;

                tabContent.Loaded += (sender2, e2) =>
                {
                    UpdateCurrentPreview();
                };

                tabControl1.Items.Add(tabItem);
                break;

            case NotifyCollectionChangedAction.Remove:
                for (int i = 0; i < tabControl1.Items.Count; i++)
                {
                    var tabItem2 = tabControl1.Items[i] as TabItem;
                    var contentControl = tabItem2.Content as ContentControl;

                    if (contentControl.Content == e.OldItems[0])
                    {
                        tabControl1.Items.RemoveAt(i--);

                        if (TaskbarManager.IsPlatformSupported
                            && !_closingFromThumbnail
                            && TaskbarManager.Instance.TabbedThumbnail.IsThumbnailPreviewAdded(tabItem2))
                        {
                            try
                            {
                                TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tabItem2);
                            }
                            catch
                            {
                            }
                        }

                        contentControl.Content = null;
                        contentControl.ContentTemplateSelector = null;

                        var headerControl = tabItem2.Header as ContentControl;

                        headerControl.Content = null;
                        headerControl.ContentTemplateSelector = null;

                        tabItem2.Header = null;
                        tabItem2.Content = null;
                        tabItem2.DataContext = null;

                        tabItem2.DragEnter -= TabItem_DragEnter;
                        tabItem2.DragOver -= TabItem_DragOver;
                        break;
                    }
                }

                break;
        }
    }

    private void TabItem_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    #region Thumbnails

    private void TabItem_Loaded(object sender, RoutedEventArgs e)
    {
        var tabItem = sender as TabItem;
        tabItem.Loaded -= TabItem_Loaded;

        if (TaskbarManager.IsPlatformSupported)
        {
            var data = (WorkspaceViewModel)((TabItem)sender).DataContext;

            var point = tabItem.TranslatePoint(new Point(), this);

            var preview = new TabbedThumbnail(this, tabItem, new Vector(0, point.Y)) { Title = data.Header };

            if (data is QDocument doc && doc.Document != null)
            {
                preview.Tooltip = doc.Document.Package.Name;
            }

            preview.TabbedThumbnailMinimized += Preview_TabbedThumbnailMinimized;
            preview.TabbedThumbnailMaximized += Preview_TabbedThumbnailMaximized;
            preview.TabbedThumbnailActivated += Preview_TabbedThumbnailActivated;
            preview.TabbedThumbnailClosed += Preview_TabbedThumbnailClosed;
            preview.TabbedThumbnailBitmapRequested += Preview_TabbedThumbnailBitmapRequested;

            preview.SetWindowIcon(Properties.Resources.Icon.GetHicon());

            try
            {
                TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(preview);
            }
            catch (COMException exc)
            {
                Trace.TraceError(exc.Message, App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (ArgumentNullException exc)
            {
                Trace.TraceError(exc.Message, App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        tabControl1.SelectedItem = tabItem;
    }

    private void Preview_TabbedThumbnailBitmapRequested(object? sender, TabbedThumbnailBitmapRequestedEventArgs e)
    {
        e.Handled = UpdateCurrentPreview(sender as TabbedThumbnail);
    }

    private void Preview_TabbedThumbnailClosed(object? sender, TabbedThumbnailEventArgs e)
    {
        if (e.WindowsControl is TabItem tabItem)
        {
            _closingFromThumbnail = true;
            ((tabItem.Content as ContentControl).Content as WorkspaceViewModel).Close.Execute(false);
            _closingFromThumbnail = false;

            var preview = sender as TabbedThumbnail;
            preview.TabbedThumbnailMinimized -= Preview_TabbedThumbnailMinimized;
            preview.TabbedThumbnailMaximized -= Preview_TabbedThumbnailMaximized;
            preview.TabbedThumbnailActivated -= Preview_TabbedThumbnailActivated;
            preview.TabbedThumbnailClosed -= Preview_TabbedThumbnailClosed;
            preview.TabbedThumbnailBitmapRequested -= Preview_TabbedThumbnailBitmapRequested;
        }
    }

    private void Preview_TabbedThumbnailActivated(object? sender, TabbedThumbnailEventArgs e)
    {
        if (e.WindowsControl is TabItem tabItem)
        {
            tabControl1.SelectedItem = tabItem;
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void Preview_TabbedThumbnailMaximized(object? sender, TabbedThumbnailEventArgs e)
    {
        WindowState = WindowState.Maximized;
        UpdateCurrentPreview();
    }

    private void Preview_TabbedThumbnailMinimized(object? sender, TabbedThumbnailEventArgs e) => WindowState = WindowState.Minimized;

    private void TabControl1_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        CollectionViewSource.GetDefaultView((DataContext as MainViewModel).DocList).MoveCurrentToPosition(tabControl1.SelectedIndex);

        if (TaskbarManager.IsPlatformSupported)
        {
            if (tabControl1.SelectedItem is TabItem tabItem && TaskbarManager.Instance.TabbedThumbnail.IsThumbnailPreviewAdded(tabItem))
            {
                TaskbarManager.Instance.TabbedThumbnail.SetActiveTab(tabItem);
            }
        }
    }

    private void Main_SizeChanged(object? sender, SizeChangedEventArgs e) => UpdateCurrentPreview();

    /// <summary>
    /// Обновить можно только текущий открытый thumbnail
    /// </summary>
    private bool UpdateCurrentPreview(TabbedThumbnail? thumbnail = null)
    {
        if (!TaskbarManager.IsPlatformSupported)
        {
            return false;
        }

        if (tabControl1.SelectedItem is TabItem tabItem)
        {
            var preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tabItem);

            if (preview != null && (preview == thumbnail || thumbnail == null))
            {
                if (tabItem.Content is not FrameworkElement element || element.ActualWidth < 1.0 || element.ActualHeight < 1.0)
                {
                    return false;
                }

                var image = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Default);

                var dv = new DrawingVisual();

                using (DrawingContext dc = dv.RenderOpen())
                {
                    var vb = new VisualBrush(element) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top };
                    var rect = new Rect(new Point(), new Size((int)element.ActualWidth, (int)element.ActualHeight));
                    dc.DrawRectangle(new SolidColorBrush(Colors.White), null, rect);
                    dc.DrawRectangle(vb, null, rect);
                }

                image.Render(dv);

                try
                {
                    preview.SetImage(image);
                    var point = element.TranslatePoint(new Point(), this);
                    preview.PeekOffset = new Vector(point.X, point.Y + 1);

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        return false;
    }

    #endregion

    private void TabItem_DragEnter(object sender, DragEventArgs e)
    {
        tabControl1.SelectedItem = sender as TabItem;
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void SearchText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ApplicationCommands.Find.CanExecute(((TextBox)sender).Text, this))
        {
            ApplicationCommands.Find.Execute(((TextBox)sender).Text, this);
        }
    }

    private void Main_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel)
        {
            return;
        }

        mainViewModel.DocList.CollectionChanged += DocList_CollectionChanged;
        UI.Execute(async () => await mainViewModel.InitializeAsync(), exc => MainViewModel.ShowError(exc));
    }

    private async void Main_Closing(object sender, CancelEventArgs e)
    {
        try
        {
            FocusManager.SetFocusedElement(this, this);

            if (DataContext is not MainViewModel mainViewModel)
            {
                return;
            }

            mainViewModel.Logger.LogInformation("Main_Closing");

            if (mainViewModel.DocList.Any())
            {
                e.Cancel = true;
                var close = await mainViewModel.TryCloseAsync();

                if (close)
                {
                    await Dispatcher.BeginInvoke(Close);
                }
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Main_Closing error: {ex}");
        }
    }

    private void Main_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel && e.Key == Key.Insert)
        {
            var cmd = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) > 0 ?
                mainViewModel.ActiveDocument?.ActiveNode?.Owner?.Add
                : mainViewModel.ActiveDocument?.ActiveNode?.Add;

            if (cmd != null)
            {
                cmd.Execute(null);
                e.Handled = true;
            }
        }
    }

    // Prevents some weird WPF automation issues (totally disables automation)
    protected override AutomationPeer OnCreateAutomationPeer() => new CustomWindowAutomationPeer(this);

    private sealed class CustomWindowAutomationPeer : FrameworkElementAutomationPeer
    {
        public CustomWindowAutomationPeer(FrameworkElement owner) : base(owner) { }

        protected override string GetNameCore() => nameof(CustomWindowAutomationPeer);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

        protected override List<AutomationPeer> GetChildrenCore() => new();
    }
}
