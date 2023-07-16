using SIQuester.ViewModel;
using SIQuester.ViewModel.Model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIQuester;

/// <summary>
/// Defines integration logic for ImportDBStorageView.
/// </summary>
public partial class ImportDBStorageView : UserControl
{
    private bool _blockFlag;

    public ImportDBStorageView() => InitializeComponent();

    private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Import();

    private void Import()
    {
        if (tree.SelectedItem is not DBNode item)
        {
            return;
        }

        if (item.Children == null)
        {
            item.PropertyChanged += (sender2, e2) =>
            {
                var item2 = (DBNode?)sender2;

                if (item2.Children.Length == 0)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ((ImportDBStorageViewModel)DataContext).SelectNodeAsync(item2);
                    }));
                }
            };
        }
        else if (item.Children.Length == 0)
        {
            ((ImportDBStorageViewModel)DataContext).SelectNodeAsync(item);
        }
    }

    private void Tree_Expanded(object sender, RoutedEventArgs e)
    {
        if (_blockFlag)
        {
            return;
        }

        var node = (DBNode)((TreeViewItem)e.OriginalSource).DataContext;

        if (node == null)
        {
            return;
        }

        if (node.Children.Length == 1 && node.Children[0] == null)
        {
            ((ImportDBStorageViewModel)DataContext).LoadChildren(node);
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (tree.SelectedItem is DBNode node)
        {
            if (node.Children.Length == 1 && node.Children[0] == null)
            {
                ((ImportDBStorageViewModel)DataContext).LoadChildren(node);
            }

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
