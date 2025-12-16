using Microsoft.Extensions.Logging;
using SIPackages;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;
using System.Reflection;

namespace SIQuester.ViewModel.Tests.Mocks;

/// <summary>
/// Test implementation of document view model factory using reflection to access internal constructor.
/// </summary>
internal sealed class TestDocumentViewModelFactory : IDocumentViewModelFactory
{
    private readonly StorageContextViewModel _storageContextViewModel;
    private readonly IClipboardService _clipboardService;
    private readonly ILoggerFactory _loggerFactory;

    public TestDocumentViewModelFactory(
        StorageContextViewModel storageContextViewModel,
        IClipboardService clipboardService,
        ILoggerFactory loggerFactory)
    {
        _storageContextViewModel = storageContextViewModel;
        _clipboardService = clipboardService;
        _loggerFactory = loggerFactory;
    }

    public QDocument CreateViewModelFor(SIDocument document, string? fileName = null)
    {
        // Use reflection to create QDocument since the constructor is internal
        var qDocumentType = typeof(QDocument);
        var constructor = qDocumentType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(SIDocument), typeof(StorageContextViewModel), typeof(IClipboardService), typeof(ILoggerFactory) },
            null);

        if (constructor == null)
        {
            throw new InvalidOperationException("Could not find QDocument constructor");
        }

        var qDocument = (QDocument)constructor.Invoke(new object[] { document, _storageContextViewModel, _clipboardService, _loggerFactory });
        qDocument.FileName = fileName ?? document.Package.Name;
        
        return qDocument;
    }
}
