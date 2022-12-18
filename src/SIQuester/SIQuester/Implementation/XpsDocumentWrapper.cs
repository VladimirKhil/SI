using SIQuester.ViewModel.PlatformSpecific;
using System.Windows.Xps.Packaging;

namespace SIQuester.Implementation;

public sealed class XpsDocumentWrapper : IXpsDocumentWrapper
{
    private readonly XpsDocument _document;

    public XpsDocumentWrapper(XpsDocument document) => _document = document;

    public object GetDocument() => _document.GetFixedDocumentSequence();

    public void Dispose() => _document.Close();
}
