using Microsoft.Extensions.Logging;
using SIPackages;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;

namespace SIQuester.ViewModel.Services;

/// <inheritdoc />
internal class DocumentViewModelFactory : IDocumentViewModelFactory
{
    private readonly StorageContextViewModel _storageContextViewModel;
    private readonly IClipboardService _clipboardService;
    private readonly ILoggerFactory _loggerFactory;

    public DocumentViewModelFactory(
        StorageContextViewModel storageContextViewModel,
        IClipboardService clipboardService,
        ILoggerFactory loggerFactory)
    {
        _storageContextViewModel = storageContextViewModel;
        _clipboardService = clipboardService;
        _loggerFactory = loggerFactory;
    }

    public QDocument CreateViewModelFor(SIDocument document, string? fileName = null) => new(document, _storageContextViewModel, _clipboardService, _loggerFactory)
    {
        FileName = fileName ?? document.Package.Name
    };
}
