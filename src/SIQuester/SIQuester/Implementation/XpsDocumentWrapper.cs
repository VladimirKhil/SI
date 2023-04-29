using SIQuester.ViewModel.PlatformSpecific;
using System.IO;
using System.Windows;
using System.Windows.Xps.Packaging;

namespace SIQuester.Implementation;

public sealed class XpsDocumentWrapper : IXpsDocumentWrapper
{
    private readonly XpsDocument _document;

    public XpsDocumentWrapper(XpsDocument document) => _document = document;

    public object? TryGetDocument()
    {
        try
        {
            return _document.GetFixedDocumentSequence();
        }
        catch (FileFormatException exc)
        {
            MessageBox.Show(exc.Message, App.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    public void Dispose() => _document.Close();
}
