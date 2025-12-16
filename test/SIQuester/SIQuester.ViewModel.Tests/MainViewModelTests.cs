using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SIPackages;
using SIQuester.Model;
using SIQuester.ViewModel.Configuration;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;
using SIQuester.ViewModel.Services;
using SIQuester.ViewModel.Tests.Mocks;
using SIStorage.Service.Contract;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for MainViewModel commands simulating user interactions.
/// </summary>
[TestFixture]
internal sealed class MainViewModelTests
{
    private MainViewModel _mainViewModel = null!;
    private IServiceProvider _serviceProvider = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "SIQuester.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<IClipboardService, ClipboardServiceMock>();
        services.AddSingleton<IPackageTemplatesRepository, PackageTemplatesRepositoryMock>();
        services.AddSingleton<StorageContextViewModel>(sp =>
        {
            var siStorageServiceClient = Substitute.For<ISIStorageServiceClient>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<StorageContextViewModel>();
            return new StorageContextViewModel(
                siStorageServiceClient,
                AppSettings.Default,
                logger);
        });
        services.AddSingleton<IDocumentViewModelFactory, DocumentViewModelFactory>();

        _serviceProvider = services.BuildServiceProvider();

        _mainViewModel = new MainViewModel(
            Array.Empty<string>(),
            new AppOptions(),
            _serviceProvider.GetRequiredService<IClipboardService>(),
            _serviceProvider,
            _serviceProvider.GetRequiredService<IDocumentViewModelFactory>(),
            _serviceProvider.GetRequiredService<ILoggerFactory>());
    }

    [TearDown]
    public void TearDown()
    {
        _mainViewModel?.Dispose();

        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region New Command

    [Test]
    public void NewCommand_Executed_ShouldOpenNewDocumentDialog()
    {
        // Arrange
        var initialDocCount = _mainViewModel.DocList.Count;

        // Act
        _mainViewModel.New.Execute(null);

        // Assert - Should add a NewViewModel to DocList
        Assert.That(_mainViewModel.DocList.Count, Is.EqualTo(initialDocCount + 1));
        Assert.That(_mainViewModel.DocList[^1], Is.InstanceOf<NewViewModel>());
    }

    [Test]
    public void NewCommand_CanExecute_ShouldAlwaysBeTrue()
    {
        // Act & Assert
        Assert.That(_mainViewModel.New.CanExecute(null), Is.True);
    }

    #endregion

    #region Open Command

    [Test]
    public async Task OpenCommand_WithValidFile_ShouldOpenDocument()
    {
        // Arrange - Create a test package file
        var document = SIDocument.Create("Test Package", "Test Author");
        var round = new Round { Name = "Test Round" };
        document.Package.Rounds.Add(round);
        
        var filePath = Path.Combine(_testDirectory, "test_open.siq");
        using (var stream = File.Create(filePath))
        {
            document.Save(stream);
        }
        document.Dispose();

        // Act - Open the file
        var qDocument = await _mainViewModel.OpenFileAsync(filePath);

        // Assert
        Assert.That(qDocument, Is.Not.Null);
        Assert.That(_mainViewModel.DocList, Does.Contain(qDocument));
        Assert.That(qDocument!.Package.Rounds.Count, Is.EqualTo(1));
        Assert.That(qDocument.Package.Rounds[0].Name, Is.EqualTo("Test Round"));
    }

    [Test]
    public async Task OpenCommand_WithNonExistentFile_ShouldHandleGracefully()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "non_existent.siq");

        // Act
        var result = await _mainViewModel.OpenFileAsync(nonExistentPath);

        // Assert - Should return null and not throw
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task OpenCommand_MultipleFiles_ShouldOpenAll()
    {
        // Arrange - Create two test packages
        var filePath1 = Path.Combine(_testDirectory, "package1.siq");
        var filePath2 = Path.Combine(_testDirectory, "package2.siq");

        var doc1 = SIDocument.Create("Package 1", "Author 1");
        using (var stream = File.Create(filePath1))
        {
            doc1.Save(stream);
        }
        doc1.Dispose();

        var doc2 = SIDocument.Create("Package 2", "Author 2");
        using (var stream = File.Create(filePath2))
        {
            doc2.Save(stream);
        }
        doc2.Dispose();

        // Act - Open both files
        var qDoc1 = await _mainViewModel.OpenFileAsync(filePath1);
        var qDoc2 = await _mainViewModel.OpenFileAsync(filePath2);

        // Assert
        Assert.That(qDoc1, Is.Not.Null);
        Assert.That(qDoc2, Is.Not.Null);
        Assert.That(_mainViewModel.DocList, Does.Contain(qDoc1));
        Assert.That(_mainViewModel.DocList, Does.Contain(qDoc2));
        Assert.That(qDoc1!.Package.Model.Name, Is.EqualTo("Package 1"));
        Assert.That(qDoc2!.Package.Model.Name, Is.EqualTo("Package 2"));
    }

    #endregion

    #region SaveAll Command

    [Test]
    public void SaveAllCommand_WithNoDocuments_ShouldNotBeExecutable()
    {
        // Act & Assert
        Assert.That(_mainViewModel.SaveAll.CanBeExecuted, Is.False);
    }

    [Test]
    public async Task SaveAllCommand_WithOpenDocuments_ShouldBeExecutable()
    {
        // Arrange - Open a document
        var document = SIDocument.Create("Test Package", "Test Author");
        var filePath = Path.Combine(_testDirectory, "test_saveall.siq");
        
        using (var stream = File.Create(filePath))
        {
            document.Save(stream);
        }
        document.Dispose();

        await _mainViewModel.OpenFileAsync(filePath);

        // Act & Assert
        Assert.That(_mainViewModel.SaveAll.CanBeExecuted, Is.True);
    }

    #endregion

    #region Document Management

    [Test]
    public async Task ActiveDocument_WhenSet_ShouldUpdateProperty()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var filePath = Path.Combine(_testDirectory, "test_active.siq");
        
        using (var stream = File.Create(filePath))
        {
            document.Save(stream);
        }
        document.Dispose();

        var qDocument = await _mainViewModel.OpenFileAsync(filePath);

        // Act
        _mainViewModel.ActiveDocument = qDocument;

        // Assert
        Assert.That(_mainViewModel.ActiveDocument, Is.EqualTo(qDocument));
    }

    [Test]
    public async Task CloseDocument_ShouldRemoveFromDocList()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var filePath = Path.Combine(_testDirectory, "test_close.siq");
        
        using (var stream = File.Create(filePath))
        {
            document.Save(stream);
        }
        document.Dispose();

        var qDocument = await _mainViewModel.OpenFileAsync(filePath);
        var initialCount = _mainViewModel.DocList.Count;

        // Act - Close the document
        await qDocument!.Close.ExecuteAsync(null);

        // Assert
        Assert.That(_mainViewModel.DocList.Count, Is.EqualTo(initialCount - 1));
        Assert.That(_mainViewModel.DocList, Does.Not.Contain(qDocument));
    }

    #endregion

    #region Workspace Management

    [Test]
    public void DocList_ShouldStartEmpty()
    {
        // Assert
        Assert.That(_mainViewModel.DocList, Is.Empty);
    }

    [Test]
    public async Task DocList_AfterOpeningDocument_ShouldContainDocument()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var filePath = Path.Combine(_testDirectory, "test_doclist.siq");
        
        using (var stream = File.Create(filePath))
        {
            document.Save(stream);
        }
        document.Dispose();

        // Act
        var qDocument = await _mainViewModel.OpenFileAsync(filePath);

        // Assert
        Assert.That(_mainViewModel.DocList, Has.Count.EqualTo(1));
        Assert.That(_mainViewModel.DocList[0], Is.EqualTo(qDocument));
    }

    #endregion

    #region Command Execution Integration

    [Test]
    public async Task FullWorkflow_CreateModifyAndSave_ShouldWork()
    {
        // Arrange - Create a new document
        var document = SIDocument.Create("Workflow Test", "Test Author");
        var filePath = Path.Combine(_testDirectory, "workflow_test.siq");

        using (var stream = File.Create(filePath))
        {
            document.Save(stream);
        }
        document.Dispose();

        // Act - Open the document
        var qDocument = await _mainViewModel.OpenFileAsync(filePath);
        Assert.That(qDocument, Is.Not.Null);

        // Modify the document
        qDocument!.Package.Model.Name = "Modified Workflow Test";
        
        // Add a round
        var round = new Round { Name = "New Round" };
        qDocument.Package.Model.Rounds.Add(round);

        // Save the document
        await qDocument.Save.ExecuteAsync(null);

        // Close the document
        await qDocument.Close.ExecuteAsync(null);

        // Reopen and verify
        var reopenedDoc = await _mainViewModel.OpenFileAsync(filePath);
        
        // Assert
        Assert.That(reopenedDoc, Is.Not.Null);
        Assert.That(reopenedDoc!.Package.Model.Name, Is.EqualTo("Modified Workflow Test"));
        Assert.That(reopenedDoc.Package.Rounds.Count, Is.EqualTo(1));
        Assert.That(reopenedDoc.Package.Rounds[0].Name, Is.EqualTo("New Round"));
    }

    #endregion
}
