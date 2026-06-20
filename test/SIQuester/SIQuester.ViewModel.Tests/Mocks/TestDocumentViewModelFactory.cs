using Microsoft.Extensions.Logging;
using SIPackages;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;
using SIStatisticsService.Contract;
using System.Reflection;

namespace SIQuester.ViewModel.Tests.Mocks;

/// <summary>
/// Test implementation of document view model factory using reflection to access internal constructor.
/// </summary>
internal sealed class TestDocumentViewModelFactory : IDocumentViewModelFactory
{
    private readonly StorageContextViewModel _storageContextViewModel;
    private readonly IPackageTemplatesRepository _packageTemplatesRepository;
    private readonly IClipboardService _clipboardService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISIStatisticsServiceClient _statisticsClient;

    public TestDocumentViewModelFactory(
        StorageContextViewModel storageContextViewModel,
        IPackageTemplatesRepository packageTemplatesRepository,
        IClipboardService clipboardService,
        ILoggerFactory loggerFactory,
        ISIStatisticsServiceClient statisticsClient)
    {
        _storageContextViewModel = storageContextViewModel;
        _packageTemplatesRepository = packageTemplatesRepository;
        _clipboardService = clipboardService;
        _loggerFactory = loggerFactory;
        _statisticsClient = statisticsClient;
    }

    public QDocument CreateViewModelFor(SIDocument document, string? fileName = null)
    {
        var qDocument = new QDocument(document, _storageContextViewModel, _packageTemplatesRepository, this, _clipboardService, _loggerFactory, _statisticsClient);
        qDocument.FileName = fileName ?? document.Package.Name;
        
        return qDocument;
    }
}
