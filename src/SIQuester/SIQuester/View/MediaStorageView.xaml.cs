using SIQuester.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SIQuester;

/// <summary>
/// Provides interaction logic for MediaStorageDialog.xaml.
/// </summary>
public partial class MediaStorageView : UserControl
{
    public MediaStorageView() => InitializeComponent();

    private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
    {
        var storage = (MediaStorageViewModel?)DataContext;
        var item = (MediaItemViewModel?)e.Item;

        if (storage == null || item == null)
        {
            return;
        }

        e.Accepted = storage.Filter.Length == 0 || item.Model.Name.Contains(storage.Filter);
    }

    private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue != null)
        {
            ((MediaStorageViewModel)e.OldValue).PropertyChanged -= MediaStorageView_PropertyChanged;
        }

        if (e.NewValue != null)
        {
            ((MediaStorageViewModel)e.NewValue).PropertyChanged += MediaStorageView_PropertyChanged;
        }
    }

    private void MediaStorageView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == null)
        {
            return;
        }

        if (e.PropertyName == nameof(MediaStorageViewModel.Filter))
        {
            ((CollectionViewSource)Resources["files"]).View.Refresh();
        }
    }
}
