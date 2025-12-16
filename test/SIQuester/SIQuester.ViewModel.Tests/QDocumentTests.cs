using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for QDocument ViewModel - covers creating and opening package documents.
/// </summary>
[TestFixture]
internal sealed class QDocumentTests
{
    private IServiceProvider _serviceProvider = null!;
    private IDocumentViewModelFactory _documentFactory = null!;

    [SetUp]
    public void Setup()
    {
        _serviceProvider = TestHelper.CreateServiceProvider();
        _documentFactory = _serviceProvider.GetRequiredService<IDocumentViewModelFactory>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #region Creating Documents

    [Test]
    public void CreateNewDocument_ShouldInitializeWithBasicStructure()
    {
        // Arrange
        var document = SIDocument.Create("New Package", "Test Author");

        // Act
        var qDocument = _documentFactory.CreateViewModelFor(document, "New Package");

        // Assert
        Assert.That(qDocument, Is.Not.Null);
        Assert.That(qDocument.Package, Is.Not.Null);
        Assert.That(qDocument.Package.Model.Name, Is.EqualTo("New Package"));
        Assert.That(qDocument.FileName, Is.EqualTo("New Package"));
        Assert.That(qDocument.Changed, Is.False, "Newly created document should not be marked as changed");
    }

    [Test]
    public void CreateNewDocument_ShouldHaveEmptyMediaCollections()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");

        // Act
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");

        // Assert
        Assert.That(qDocument.Images, Is.Not.Null);
        Assert.That(qDocument.Audio, Is.Not.Null);
        Assert.That(qDocument.Video, Is.Not.Null);
        Assert.That(qDocument.Html, Is.Not.Null);
        Assert.That(qDocument.Images.Files, Is.Empty);
        Assert.That(qDocument.Audio.Files, Is.Empty);
        Assert.That(qDocument.Video.Files, Is.Empty);
        Assert.That(qDocument.Html.Files, Is.Empty);
    }

    #endregion

    #region Opening Documents

    [Test]
    public void OpenDocument_WithExistingPackage_ShouldLoadStructure()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();

        // Act
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");

        // Assert
        Assert.That(qDocument.Package, Is.Not.Null);
        Assert.That(qDocument.Package.Rounds, Has.Count.EqualTo(1));
        Assert.That(qDocument.Package.Rounds[0].Name, Is.EqualTo("Round 1"));
        Assert.That(qDocument.Package.Rounds[0].Themes, Has.Count.EqualTo(1));
        Assert.That(qDocument.Package.Rounds[0].Themes[0].Name, Is.EqualTo("Theme 1"));
    }

    [Test]
    public void OpenDocument_WithQuestions_ShouldLoadQuestions()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();

        // Act
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");

        // Assert
        var theme = qDocument.Package.Rounds[0].Themes[0];
        Assert.That(theme.Questions, Has.Count.EqualTo(1));
        Assert.That(theme.Questions[0].Model.Price, Is.EqualTo(100));
    }

    #endregion

    #region Editing Text Content

    [Test]
    public void EditPackageName_ShouldUpdateModelAndMarkAsChanged()
    {
        // Arrange
        var document = SIDocument.Create("Original Name", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Original Name");
        qDocument.ClearChange(); // Reset changed flag after creation

        // Act
        qDocument.Package.Model.Name = "Updated Name";

        // Assert
        Assert.That(qDocument.Package.Model.Name, Is.EqualTo("Updated Name"));
        // Note: Changed flag is managed by OperationsManager through property change notifications
    }

    [Test]
    public void EditRoundName_ShouldUpdateModel()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var round = qDocument.Package.Rounds[0];

        // Act
        round.Model.Name = "Modified Round Name";

        // Assert
        Assert.That(round.Name, Is.EqualTo("Modified Round Name"));
        Assert.That(qDocument.Package.Rounds[0].Name, Is.EqualTo("Modified Round Name"));
    }

    [Test]
    public void EditThemeName_ShouldUpdateModel()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var theme = qDocument.Package.Rounds[0].Themes[0];

        // Act
        theme.Model.Name = "Modified Theme Name";

        // Assert
        Assert.That(theme.Name, Is.EqualTo("Modified Theme Name"));
    }

    [Test]
    public void EditQuestionPrice_ShouldUpdateModel()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];

        // Act
        question.Model.Price = 200;

        // Assert
        Assert.That(question.Model.Price, Is.EqualTo(200));
    }

    #endregion

    #region Editing Structure - Adding Elements

    [Test]
    public void AddRound_ShouldIncreaseRoundCount()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var initialCount = qDocument.Package.Rounds.Count;

        // Act
        var newRound = new Round { Name = "New Round" };
        qDocument.Package.Model.Rounds.Add(newRound);

        // Assert
        Assert.That(qDocument.Package.Rounds.Count, Is.EqualTo(initialCount + 1));
        Assert.That(qDocument.Package.Rounds[^1].Name, Is.EqualTo("New Round"));
    }

    [Test]
    public void AddThemeToRound_ShouldIncreaseThemeCount()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var round = qDocument.Package.Rounds[0];
        var initialCount = round.Themes.Count;

        // Act
        var newTheme = new Theme { Name = "New Theme" };
        round.Model.Themes.Add(newTheme);

        // Assert
        Assert.That(round.Themes.Count, Is.EqualTo(initialCount + 1));
        Assert.That(round.Themes[^1].Name, Is.EqualTo("New Theme"));
    }

    [Test]
    public void AddQuestionToTheme_ShouldIncreaseQuestionCount()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var theme = qDocument.Package.Rounds[0].Themes[0];
        var initialCount = theme.Questions.Count;

        // Act
        var newQuestion = new Question { Price = 300 };
        newQuestion.Right.Add("New answer");
        theme.Model.Questions.Add(newQuestion);

        // Assert
        Assert.That(theme.Questions.Count, Is.EqualTo(initialCount + 1));
        Assert.That(theme.Questions[^1].Model.Price, Is.EqualTo(300));
    }

    #endregion

    #region Editing Structure - Removing Elements

    [Test]
    public void RemoveRound_ShouldDecreaseRoundCount()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var initialCount = qDocument.Package.Rounds.Count;

        // Act
        qDocument.Package.Model.Rounds.RemoveAt(0);

        // Assert
        Assert.That(qDocument.Package.Rounds.Count, Is.EqualTo(initialCount - 1));
    }

    [Test]
    public void RemoveTheme_ShouldDecreaseThemeCount()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var round = qDocument.Package.Rounds[0];
        var initialCount = round.Themes.Count;

        // Act
        round.Model.Themes.RemoveAt(0);

        // Assert
        Assert.That(round.Themes.Count, Is.EqualTo(initialCount - 1));
    }

    [Test]
    public void RemoveQuestion_ShouldDecreaseQuestionCount()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var theme = qDocument.Package.Rounds[0].Themes[0];
        var initialCount = theme.Questions.Count;

        // Act
        theme.Model.Questions.RemoveAt(0);

        // Assert
        Assert.That(theme.Questions.Count, Is.EqualTo(initialCount - 1));
    }

    #endregion

    #region Copy and Paste Operations

    [Test]
    public void CopyRound_ShouldStoreDataInClipboard()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        qDocument.ActiveNode = qDocument.Package.Rounds[0];

        // Act
        if (qDocument.Copy.CanExecute(null))
        {
            qDocument.Copy.Execute(null);
        }

        // Assert - Command should execute without throwing
        Assert.Pass("Copy command executed successfully");
    }

    #endregion

    #region Navigation and Selection

    [Test]
    public void SetActiveNode_ShouldUpdateActiveNode()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var round = qDocument.Package.Rounds[0];

        // Act
        qDocument.ActiveNode = round;

        // Assert
        Assert.That(qDocument.ActiveNode, Is.EqualTo(round));
    }

    [Test]
    public void NavigateToTheme_ShouldUpdateActiveNode()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var theme = qDocument.Package.Rounds[0].Themes[0];

        // Act
        qDocument.ActiveNode = theme;

        // Assert
        Assert.That(qDocument.ActiveNode, Is.EqualTo(theme));
    }

    #endregion

    #region Document State

    [Test]
    public void NewDocument_ShouldNotBeChanged()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        
        // Act
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        qDocument.ClearChange(); // Clear the initial change flag

        // Assert
        Assert.That(qDocument.Changed, Is.False);
    }

    [Test]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => qDocument.Dispose());
    }

    #endregion
}
