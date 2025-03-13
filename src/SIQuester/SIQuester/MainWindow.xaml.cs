using MahApps.Metro.Controls;
using Microsoft.Extensions.Logging;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Utils;

namespace SIQuester;

/// <summary>
/// Defines main application window.
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow() => InitializeComponent();

    /// <summary>
    /// Performs manual binding between DocList collection and tabs. TODO: switch to automatic binding in the future.
    /// </summary>
    private void DocList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null || e.NewItems.Count == 0)
                {
                    break;
                }

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

                tabControl1.Items.Add(tabItem);

                if (sender != null)
                {
                    CollectionViewSource.GetDefaultView(sender).MoveCurrentToLast();
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null || e.OldItems.Count == 0)
                {
                    break;
                }

                for (int i = 0; i < tabControl1.Items.Count; i++)
                {
                    if (tabControl1.Items[i] is TabItem tabItem2
                        && tabItem2?.Content is ContentControl contentControl
                        && contentControl.Content == e.OldItems[0])
                    {
                        tabControl1.Items.RemoveAt(i--);

                        contentControl.Content = null;
                        contentControl.ContentTemplateSelector = null;

                        if (tabItem2.Header is ContentControl headerControl)
                        {
                            headerControl.Content = null;
                            headerControl.ContentTemplateSelector = null;
                        }

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

    private void TabItem_Loaded(object sender, RoutedEventArgs e)
    {
        var tabItem = (TabItem)sender;
        tabItem.Loaded -= TabItem_Loaded;

        tabControl1.SelectedItem = tabItem;
    }

    private void TabControl1_SelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        CollectionViewSource.GetDefaultView(((MainViewModel)DataContext).DocList).MoveCurrentToPosition(tabControl1.SelectedIndex);

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

    private void New_Executed(object sender, ExecutedRoutedEventArgs e) => ((MainViewModel)DataContext).New.Execute(null);

    private void Open_Executed(object sender, ExecutedRoutedEventArgs e) => ((MainViewModel) DataContext).Open.Execute(e.Parameter);

    private void Help_Executed(object sender, ExecutedRoutedEventArgs e) => ((MainViewModel)DataContext).Help.Execute(null);

    private void Close_Executed(object sender, ExecutedRoutedEventArgs e) => ((MainViewModel)DataContext).Close.ExecuteAsync(null);

    private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e) => ((MainViewModel)DataContext).ActiveDocument?.SaveAs.ExecuteAsync(null);

    private void Copy_Executed(object sender, ExecutedRoutedEventArgs e) => ((MainViewModel)DataContext).ActiveDocument?.Copy.Execute(null);

    private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var doc = ((MainViewModel)DataContext).ActiveDocument;

        if (doc == null)
        {
            return;
        }

        var activeNode = doc.ActiveNode;
        var activeItem = doc.ActiveItem;

        var contentItemViewModel = activeItem as ContentItemsViewModel;

        if (contentItemViewModel == null && activeNode is QuestionViewModel question)
        {
            foreach (var parameterRecord in question.Parameters)
            {
                if (parameterRecord.Key == QuestionParameterNames.Question)
                {
                    contentItemViewModel = parameterRecord.Value.ContentValue;
                    break;
                }
            }
        }

        if (contentItemViewModel == null)
        {
            doc.Paste.Execute(null);
            return;
        }

        var files = Clipboard.GetFileDropList();

        if (files.Count > 0)
        {
            foreach (var file in files)
            {
                if (file != null)
                {
                    contentItemViewModel.TryImportMedia(file);
                }
            }

            return;
        }

        if (Clipboard.ContainsImage())
        {
            var bitmap = Clipboard.GetImage();
            var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);

            var tempMediaDirectory = Path.Combine(Path.GetTempPath(), AppSettings.ProductName, AppSettings.MediaFolderName);
            Directory.CreateDirectory(tempMediaDirectory);

            var fileName = Path.Combine(tempMediaDirectory, $"{Guid.NewGuid()}.png");
            File.WriteAllBytes(fileName, stream.ToArray());

            contentItemViewModel.TryImportMedia(fileName);
            return;
        }

        doc.Paste.Execute(null);
    }
}
