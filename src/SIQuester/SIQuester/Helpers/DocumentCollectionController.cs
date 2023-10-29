using SIQuester.ViewModel;
using System.ComponentModel;
using System.Windows.Data;

namespace SIQuester.Helpers;

/// <summary>
/// Manages main view document collection.
/// </summary>
internal static class DocumentCollectionController
{
    internal static void AttachTo(MainViewModel mainViewModel)
    {
        var collectionView = CollectionViewSource.GetDefaultView(mainViewModel.DocList);
        collectionView.CurrentChanged += (sender, e) => mainViewModel.ActiveDocument = ((ICollectionView?)sender)?.CurrentItem as QDocument;
    }
}
