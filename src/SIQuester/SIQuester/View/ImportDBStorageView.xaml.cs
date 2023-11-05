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

        if (!node.ChildrenLoaded)
        {
            ((ImportDBStorageViewModel)DataContext).LoadChildren(node);
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (tree.SelectedItem is DBNode node)
        {
            if (!node.ChildrenLoaded)
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

    private void Import()
    {
        if (tree.SelectedItem is not DBNode node)
        {
            return;
        }

        if (node.ChildrenLoaded && node.Children.Length == 0)
        {
            ((ImportDBStorageViewModel)DataContext).SelectNodeAsync(node);
        }
    }
}
