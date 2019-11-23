using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SIQuester.ViewModel;

namespace SIQuester
{
    /// <summary>
    /// Логика взаимодействия для ImportDBStorageView.xaml
    /// </summary>
    public partial class ImportDBStorageView : UserControl
    {
        private bool _blockFlag;

        public ImportDBStorageView()
        {
            InitializeComponent();
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Import();
        }

        private void Import()
        {
            if (tree.SelectedItem is DBNode item)
            {
                if (item.Children == null)
                {
                    item.PropertyChanged += (sender2, e2) =>
                    {
                        var item2 = (DBNode)sender2;
                        if (item2.Children.Length == 0)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ((ImportDBStorageViewModel)DataContext).Select(item2);
                            }));
                        }
                    };
                }
                else if (item.Children.Length == 0)
                {
                    ((ImportDBStorageViewModel)DataContext).Select(item);
                }
            }
        }

        private void Tree_Expanded(object sender, RoutedEventArgs e)
        {
            if (_blockFlag)
                return;

            var node = (DBNode)((TreeViewItem)e.OriginalSource).DataContext;
            if (node == null)
                return;

            if (node.Children.Length == 1 && node.Children[0] == null)
                ((ImportDBStorageViewModel)DataContext).LoadChildren(node);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (tree.SelectedItem is DBNode node)
            {
                if (node.Children.Length == 1 && node.Children[0] == null)
                    ((ImportDBStorageViewModel)DataContext).LoadChildren(node);

                _blockFlag = true;
                try
                {
                    node.IsExpanded = true;
                }
                finally
                {
                    _blockFlag = false;
                }
            }

            Import();
        }
    }
}
