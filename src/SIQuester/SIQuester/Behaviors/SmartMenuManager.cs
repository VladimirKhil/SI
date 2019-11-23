using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using SIQuester.ViewModel;
using System.Diagnostics;

namespace SIQuester
{
    public static class SmartMenuManager
    {
        public static bool GetSmartMenu(DependencyObject obj)
        {
            return (bool)obj.GetValue(SmartMenuProperty);
        }

        public static void SetSmartMenu(DependencyObject obj, bool value)
        {
            obj.SetValue(SmartMenuProperty, value);
        }

        // Using a DependencyProperty as the backing store for SmartMenu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SmartMenuProperty =
            DependencyProperty.RegisterAttached("SmartMenu", typeof(bool), typeof(SmartMenuManager), new UIPropertyMetadata(false, SmartMenuChanged));

        private static void SmartMenuChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem)
            {
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
        }

        private static void TreeViewItem_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ActionMenuViewModel.Instance.PlacementTarget == sender)
            {
                ActionMenuViewModel.Instance.IsOpen = false;
            }
        }

        private static void TreeViewItem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is TreeViewItem treeViewItem))
                treeViewItem = (TreeViewItem)sender;

            if (!ActionMenuViewModel.Instance.IsOpen)
            {
                ActionMenuViewModel.Instance.PlacementTarget = treeViewItem;
            }

            ActionMenuViewModel.Instance.IsOpen = true;
            treeViewItem.IsSelected = true;

            e.Handled = true;
        }

        private static void TreeViewItem_LostFocus(object sender, RoutedEventArgs e)
        {
            ActionMenuViewModel.Instance.IsOpen = false;
        }
        
        public static bool GetSecondMenu(DependencyObject obj)
        {
            return (bool)obj.GetValue(SecondMenuProperty);
        }

        public static void SetSecondMenu(DependencyObject obj, bool value)
        {
            obj.SetValue(SecondMenuProperty, value);
        }

        // Using a DependencyProperty as the backing store for SecondMenu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SecondMenuProperty =
            DependencyProperty.RegisterAttached("SecondMenu", typeof(bool), typeof(SmartMenuManager), new UIPropertyMetadata(false, SecondMenuChanged));

        private static void SecondMenuChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is FrameworkElement control)
            {
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
                        var doc = ((MainViewModel)App.Current.MainWindow.DataContext).ActiveDocument;
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
            }
        }

        private static void Control_GotFocus(object sender, RoutedEventArgs e)
        {
            var control = sender as FrameworkElement;

            var doc = ((MainViewModel)App.Current.MainWindow.DataContext).ActiveDocument;
            doc.ActiveItem = control.DataContext;

            ActionMenuViewModel.Instance.PlacementTarget = control;
            ActionMenuViewModel.Instance.IsOpen = true;
        }

        private static void Control_LostFocus(object sender, RoutedEventArgs e)
        {
            ActionMenuViewModel.Instance.IsOpen = false;

            var doc = ((MainViewModel)App.Current.MainWindow.DataContext).ActiveDocument;

            if (doc != null)
                doc.ActiveItem = null;
        }
    }
}
