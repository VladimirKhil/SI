using Microsoft.Extensions.Logging;
using SIPackages;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;
using SIStatisticsService.Contract;

namespace SIQuester.ViewModel.Services;

/// <inheritdoc />
internal class DocumentViewModelFactory : IDocumentViewModelFactory
{
    private readonly StorageContextViewModel _storageContextViewModel;
    private readonly IPackageTemplatesRepository _packageTemplatesRepository;
    private readonly IClipboardService _clipboardService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISIStatisticsServiceClient _statisticsClient;

    public DocumentViewModelFactory(
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

    public QDocument CreateViewModelFor(SIDocument document, string? fileName = null) => new(
        document,
        _storageContextViewModel,
        _packageTemplatesRepository,
        this,
        _clipboardService,
        _loggerFactory,
        _statisticsClient)
    {
        FileName = fileName ?? document.Package.Name
    };
}
