using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;
using SIQuester.ViewModel.Tests.Mocks;
using SIStorage.Service.Contract;

namespace SIQuester.ViewModel.Tests.Helpers;

/// <summary>
/// Helper class for creating test instances.
/// </summary>
internal static class TestHelper
{
    /// <summary>
    /// Creates a test service provider with mock dependencies.
    /// </summary>
    public static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<IClipboardService, ClipboardServiceMock>();
        services.AddSingleton<IPackageTemplatesRepository, PackageTemplatesRepositoryMock>();
        services.AddSingleton<StorageContextViewModel>(sp => CreateStorageContextViewModel(sp));
        services.AddSingleton<IDocumentViewModelFactory, TestDocumentViewModelFactory>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a simple test package with basic structure.
    /// </summary>
    public static SIDocument CreateSimpleTestPackage()
    {
        var document = SIDocument.Create("Test Package", "Test Author");
        
        // Add a round with themes and questions
        var round = new Round { Name = "Round 1" };
        var theme = new Theme { Name = "Theme 1" };
        var question = new Question { Price = 100 };
        
        // Add question content using Script and Steps
        question.Script = new Script();
        question.Script.Steps.Add(new Step
        {
            Type = StepTypes.ShowContent,
            Parameters =
            {
                [StepParameterNames.Content] = new StepParameter
                {
                    Type = StepParameterTypes.Content,
                    ContentValue = new List<ContentItem>
                    {
                        new() { Type = ContentTypes.Text, Value = "Test question text" }
                    }
                }
            }
        });
        
        question.Right.Add("Test answer");
        
        theme.Questions.Add(question);
        round.Themes.Add(theme);
        document.Package.Rounds.Add(round);
        
        return document;
    }

    /// <summary>
    /// Creates a document view model factory for testing.
    /// </summary>
    public static IDocumentViewModelFactory CreateDocumentViewModelFactory(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IDocumentViewModelFactory>();
    }

    private static StorageContextViewModel CreateStorageContextViewModel(IServiceProvider serviceProvider)
    {
        // Mock the ISIStorageServiceClient using NSubstitute
        var siStorageServiceClient = Substitute.For<ISIStorageServiceClient>();
        
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<StorageContextViewModel>();
        
        return new StorageContextViewModel(
            siStorageServiceClient,
            AppSettings.Default,
            logger);
    }
}
