using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SIQuester.ViewModel
{
    public static class TreeViewHelper
    {
        public static object GetSelectedItem(DependencyObject obj)
        {
            return (object)obj.GetValue(SelectedItemProperty);
        }

        public static void SetSelectedItem(DependencyObject obj, object value)
        {
            obj.SetValue(SelectedItemProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached("SelectedItem", typeof(object), typeof(TreeViewHelper), new UIPropertyMetadata(null));
        
        public static bool GetWatchSelection(DependencyObject obj)
        {
            return (bool)obj.GetValue(WatchSelectionProperty);
        }

        public static void SetWatchSelection(DependencyObject obj, bool value)
        {
            obj.SetValue(WatchSelectionProperty, value);
        }

        // Using a DependencyProperty as the backing store for WatchSelection.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WatchSelectionProperty =
            DependencyProperty.RegisterAttached("WatchSelection", typeof(bool), typeof(TreeViewHelper), new UIPropertyMetadata(false, WatchSelectionChanged));
        
        public static void WatchSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var treeView = (TreeView)d;
            if (treeView != null)
            {
                if ((bool)e.NewValue)
                {
                    treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
                    treeView.AddHandler(TreeViewItem.SelectedEvent, (RoutedEventHandler)Item_Selected);
                }
                else
                {
                    treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                    treeView.RemoveHandler(TreeViewItem.SelectedEvent, (RoutedEventHandler)Item_Selected);
                }
            }
        }

        private static void Item_Selected(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)e.OriginalSource;
            if (item != null)
                item.BringIntoView();
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeView = (TreeView)sender;
            SetSelectedItem(treeView, treeView.SelectedItem);
        }
    }
}
