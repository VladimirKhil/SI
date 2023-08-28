using SIQuester.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester;

public static class SmartMenuManager
{
    public static bool GetSmartMenu(DependencyObject obj) => (bool)obj.GetValue(SmartMenuProperty);

    public static void SetSmartMenu(DependencyObject obj, bool value) => obj.SetValue(SmartMenuProperty, value);

    public static readonly DependencyProperty SmartMenuProperty =
        DependencyProperty.RegisterAttached("SmartMenu", typeof(bool), typeof(SmartMenuManager), new UIPropertyMetadata(false, SmartMenuChanged));

    private static void SmartMenuChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TreeViewItem treeViewItem)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            treeViewItem.GotFocus += TreeViewItem_GotFocus;
            treeViewItem.LostFocus += TreeViewItem_LostFocus;
            treeViewItem.Unloaded += TreeViewItem_Unloaded;
        }
        else
        {
            treeViewItem.GotFocus -= TreeViewItem_GotFocus;
            treeViewItem.LostFocus -= TreeViewItem_LostFocus;
            treeViewItem.Unloaded -= TreeViewItem_Unloaded;
        }
    }

    private static void TreeViewItem_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ActionMenuViewModel.Instance.PlacementTarget == sender)
        {
            ActionMenuViewModel.Instance.IsOpen = false;
            ActionMenuViewModel.Instance.PlacementTarget = null;
        }
    }

    private static void TreeViewItem_GotFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TreeViewItem treeViewItem)
        {
            treeViewItem = (TreeViewItem)sender;
        }

        if (!ActionMenuViewModel.Instance.IsOpen)
        {
            ActionMenuViewModel.Instance.PlacementTarget = treeViewItem;
        }

        ActionMenuViewModel.Instance.IsOpen = true;
        treeViewItem.IsSelected = true;

        e.Handled = true;
    }

    private static void TreeViewItem_LostFocus(object sender, RoutedEventArgs e) => ActionMenuViewModel.Instance.IsOpen = false;

    public static bool GetSecondMenu(DependencyObject obj) => (bool)obj.GetValue(SecondMenuProperty);

    public static void SetSecondMenu(DependencyObject obj, bool value) => obj.SetValue(SecondMenuProperty, value);

    public static readonly DependencyProperty SecondMenuProperty =
        DependencyProperty.RegisterAttached("SecondMenu", typeof(bool), typeof(SmartMenuManager), new UIPropertyMetadata(false, SecondMenuChanged));

    private static void SecondMenuChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not FrameworkElement control)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            control.GotFocus += Control_GotFocus;
            control.LostFocus += Control_LostFocus;
            control.Unloaded += Control_Unloaded;
        }
        else
        {
            control.GotFocus -= Control_GotFocus;
            control.LostFocus -= Control_LostFocus;
            control.Unloaded -= Control_Unloaded;

            if (ActionMenuViewModel.Instance.PlacementTarget == control)
            {
                ActionMenuViewModel.Instance.IsOpen = false;
                ActionMenuViewModel.Instance.PlacementTarget = null;
                var doc = ((MainViewModel)Application.Current.MainWindow.DataContext).ActiveDocument;

                if (doc != null)
                {
                    doc.ActiveItem = null;
                }
            }
        }
    }

    private static void Control_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ActionMenuViewModel.Instance.PlacementTarget == sender)
        {
            ActionMenuViewModel.Instance.IsOpen = false;
            ActionMenuViewModel.Instance.PlacementTarget = null;
        }
    }

    private static void Control_GotFocus(object sender, RoutedEventArgs e)
    {
        var control = (FrameworkElement)sender;

        var doc = ((MainViewModel)Application.Current.MainWindow.DataContext).ActiveDocument;

        if (doc == null)
        {
            return;
        }

        var context = control.DataContext;

        // For backward compatibility
        // TODO: remove after switching to new format
        if (context is QuestionViewModel questionViewModel)
        {
            context = questionViewModel.Scenario;
        }

        doc.ActiveItem = context;

        ActionMenuViewModel.Instance.PlacementTarget = control;
        ActionMenuViewModel.Instance.IsOpen = true;
    }

    private static void Control_LostFocus(object sender, RoutedEventArgs e)
    {
        ActionMenuViewModel.Instance.IsOpen = false;
        ActionMenuViewModel.Instance.PlacementTarget = null;

        var doc = ((MainViewModel)Application.Current.MainWindow.DataContext).ActiveDocument;

        if (doc != null)
        {
            doc.ActiveItem = null;
        }
    }
}
