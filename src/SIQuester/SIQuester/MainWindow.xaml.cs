using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Taskbar;
using Notions;
using SIQuester.Converters;
using SIQuester.Properties;
using SIQuester.ViewModel;
using System.Windows.Threading;
using SIQuester.Model;
using MahApps.Metro.Controls;

namespace SIQuester
{
    /// <summary>
    /// Главное окно приложения
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private bool _closingFromThumbnail = false;
        private DispatcherTimer _autoSaveTimer;

        public MainWindow()
        {
            InitializeComponent();

            if (AppSettings.Default.AutoSave)
                _autoSaveTimer = new DispatcherTimer(TimeSpan.FromSeconds(20), DispatcherPriority.Background, AutoSave, Dispatcher);
        }

        private void AutoSave(object sender, EventArgs args)
        {
            var main = (MainViewModel)DataContext;
            if (main != null)
                main.AutoSave();
        }

        /// <summary>
        /// Поскольку TabControl при привязке к коллекции элементов ведёт себя достаточно странно, обеспечим создание и уничтожение вкладок самостоятельно
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DocList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    var tabContent = new ContentControl { Content = e.NewItems[0], ContentTemplateSelector = (DataTemplateSelector)Application.Current.Resources["ContentSelector"] };
                    var tabItem = new TabItem()
                    {
                        Header = new ContentControl { Content = e.NewItems[0], ContentTemplate = (DataTemplate)Resources["TabItemHeaderTemplate"] },
                        Content = tabContent,
                        DataContext = e.NewItems[0],
                        Padding = new Thickness(3,1,1,1),
                        BorderThickness = new Thickness(10)
                    };

                    tabItem.DragEnter += new DragEventHandler(TabItem_DragEnter);
                    tabItem.DragOver += TabItem_DragOver;
                    tabItem.Loaded += new RoutedEventHandler(TabItem_Loaded);
                    tabContent.Loaded += (sender2, e2) =>
                        {
                            UpdateCurrentPreview();
                        };

                    tabControl1.Items.Add(tabItem);
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < tabControl1.Items.Count; i++)
                    {
                        var tabItem2 = tabControl1.Items[i] as TabItem;
                        var contentControl = tabItem2.Content as ContentControl;
                        if (contentControl.Content == e.OldItems[0])
                        {
                            tabControl1.Items.RemoveAt(i--);
                            if (TaskbarManager.IsPlatformSupported && !_closingFromThumbnail)
                            {
                                try
                                {
                                    TaskbarManager.Instance.TabbedThumbnail.RemoveThumbnailPreview(tabItem2);
                                }
                                catch
                                {
                                }
                            }

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
            if (TaskbarManager.IsPlatformSupported)
            {
                var data = (WorkspaceViewModel)((TabItem)sender).DataContext;
                tabItem.Loaded -= TabItem_Loaded;

                var point = tabItem.TranslatePoint(new System.Windows.Point(), this);

                var preview = new TabbedThumbnail(this, tabItem, new Vector(0, point.Y)) { Title = data.Header };
                if (data is QDocument doc && doc.Document != null)
                    preview.Tooltip = doc.Document.Package.Name;

                preview.TabbedThumbnailMinimized += Preview_TabbedThumbnailMinimized;
                preview.TabbedThumbnailMaximized += Preview_TabbedThumbnailMaximized;
                preview.TabbedThumbnailActivated += Preview_TabbedThumbnailActivated;
                preview.TabbedThumbnailClosed += Preview_TabbedThumbnailClosed;
                preview.TabbedThumbnailBitmapRequested += Preview_TabbedThumbnailBitmapRequested;
                
                preview.SetWindowIcon(Properties.Resources.Icon.GetHicon());

                TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(preview);
            }

            tabControl1.SelectedItem = tabItem;
        }

        private void Preview_TabbedThumbnailBitmapRequested(object sender, TabbedThumbnailBitmapRequestedEventArgs e)
        {
            e.Handled = UpdateCurrentPreview(sender as TabbedThumbnail);
        }

        private void Preview_TabbedThumbnailClosed(object sender, TabbedThumbnailEventArgs e)
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

        private void Preview_TabbedThumbnailActivated(object sender, TabbedThumbnailEventArgs e)
        {
            if (e.WindowsControl is TabItem tabItem)
                tabControl1.SelectedItem = tabItem;

            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;

            Activate();
        }

        private void Preview_TabbedThumbnailMaximized(object sender, TabbedThumbnailEventArgs e)
        {
            WindowState = WindowState.Maximized;
            UpdateCurrentPreview();
        }

        private void Preview_TabbedThumbnailMinimized(object sender, TabbedThumbnailEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void Main_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCurrentPreview();
        }

        /// <summary>
        /// Обновить можно только текущий открытый thumbnail
        /// </summary>
        private bool UpdateCurrentPreview(TabbedThumbnail thumbnail = null)
        {
            if (!TaskbarManager.IsPlatformSupported)
                return false;

            if (tabControl1.SelectedItem is TabItem tabItem)
            {
                var preview = TaskbarManager.Instance.TabbedThumbnail.GetThumbnailPreview(tabItem);

                if (preview != null && (preview == thumbnail || thumbnail == null))
                {
                    if (!(tabItem.Content is FrameworkElement element) || element.ActualWidth < 1.0 || element.ActualHeight < 1.0)
                        return false;

                    var image = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Default);

                    var dv = new DrawingVisual();

                    using (DrawingContext dc = dv.RenderOpen())
                    {
                        var vb = new VisualBrush(element) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top };
                        var rect = new Rect(new System.Windows.Point(), new Size((int)element.ActualWidth, (int)element.ActualHeight));
                        dc.DrawRectangle(new SolidColorBrush(Colors.White), null, rect);
                        dc.DrawRectangle(vb, null, rect);
                    }

                    image.Render(dv);
                    try
                    {
                        preview.SetImage(image);
                        var point = element.TranslatePoint(new System.Windows.Point(), this);
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
            if (e.Key == Key.Enter && ApplicationCommands.Find.CanExecute((sender as TextBox).Text, this))
            {
                ApplicationCommands.Find.Execute((sender as TextBox).Text, this);
            }
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            var mainViewModel = DataContext as MainViewModel;
            mainViewModel.DocList.CollectionChanged += DocList_CollectionChanged;
            mainViewModel.Initialize();
        }

        private void Main_Closed(object sender, EventArgs e)
        {
            if (DataContext is IDisposable disposable)
                disposable.Dispose();

            if (_autoSaveTimer != null)
            {
                _autoSaveTimer.Stop();
                _autoSaveTimer = null;
            }
        }

        private async void Main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FocusManager.SetFocusedElement(this, this);
            if (DataContext is MainViewModel mainViewModel && mainViewModel.DocList.Any())
            {
                e.Cancel = true;

                var result = await mainViewModel.DisposeRequest();
                if (result)
                {
                    await Dispatcher.BeginInvoke((Action)Close);
                }
            }
        }

        private void Main_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                if (e.Key == Key.Insert)
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
        }
    }
}
