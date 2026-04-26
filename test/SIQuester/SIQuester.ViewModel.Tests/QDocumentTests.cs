using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;
using System.Text.Json;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for QDocument ViewModel - covers creating, opening, and editing package documents.
/// Tests simulate user behavior by working with the document through commands and property changes.
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
        Assert.That(qDocument.Package.Rounds[0].Model.Name, Is.EqualTo("Round 1"));
        Assert.That(qDocument.Package.Rounds[0].Themes, Has.Count.EqualTo(1));
        Assert.That(qDocument.Package.Rounds[0].Themes[0].Model.Name, Is.EqualTo("Theme 1"));
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
    public void EditPackageName_ShouldUpdateModel()
    {
        // Arrange
        var document = SIDocument.Create("Original Name", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Original Name");

        // Act
        qDocument.Package.Model.Name = "Updated Name";

        // Assert
        Assert.That(qDocument.Package.Model.Name, Is.EqualTo("Updated Name"));
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
        Assert.That(round.Model.Name, Is.EqualTo("Modified Round Name"));
        Assert.That(qDocument.Package.Rounds[0].Model.Name, Is.EqualTo("Modified Round Name"));
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
        Assert.That(theme.Model.Name, Is.EqualTo("Modified Theme Name"));
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

    [Test]
    public void ChangeQuestionType_ShouldPreserveAnswerDeviation()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];

        question.Parameters.AddParameter(QuestionParameterNames.AnswerType, new global::SIQuester.ViewModel.StepParameterViewModel(question, new StepParameter
        {
            Type = StepParameterTypes.Simple,
            SimpleValue = StepParameterValues.SetAnswerTypeType_Number
        }));

        question.Parameters.AddParameter(QuestionParameterNames.AnswerDeviation, new global::SIQuester.ViewModel.StepParameterViewModel(question, new StepParameter
        {
            Type = StepParameterTypes.Simple,
            SimpleValue = "7"
        }));

        // Act
        question.TypeName = QuestionTypes.Secret;

        // Assert
        Assert.That(question.Parameters.Model.TryGetValue(QuestionParameterNames.AnswerDeviation, out var answerDeviation), Is.True);
        Assert.That(answerDeviation?.SimpleValue, Is.EqualTo("7"));
    }

    [Test]
    public void DeserializeClipboardQuestionData_ShouldSucceed()
    {
        // Arrange
        const string json = "{\"ItemLevel\":3,\"ItemData\":\"\\u003Cquestion price=\\u0022300\\u0022\\u003E\\u003Cparams\\u003E\\u003Cparam name=\\u0022question\\u0022 type=\\u0022content\\u0022\\u003E\\u003Citem waitForFinish=\\u0022False\\u0022\\u003E\\u0412\\u043E\\u043F\\u0440\\u043E\\u0441 \\u0441 \\u043E\\u0442\\u0432\\u0435\\u0442\\u043E\\u043C \\u0432 \\u0432\\u0438\\u0434\\u0435 \\u0442\\u043E\\u0447\\u043A\\u0438 \\u043D\\u0430 \\u0438\\u0437\\u043E\\u0431\\u0440\\u0430\\u0436\\u0435\\u043D\\u0438\\u0438. \\u041D\\u0430\\u0439\\u0434\\u0438\\u0442\\u0435 \\u043B\\u043E\\u0434\\u043A\\u0443\\u003C/item\\u003E\\u003Citem type=\\u0022image\\u0022 isRef=\\u0022True\\u0022\\u003Esample-boat-400x300.png\\u003C/item\\u003E\\u003C/param\\u003E\\u003Cparam name=\\u0022answerType\\u0022\\u003Epoint\\u003C/param\\u003E\\u003Cparam name=\\u0022answer\\u0022 type=\\u0022content\\u0022\\u003E\\u003Citem type=\\u0022image\\u0022 isRef=\\u0022True\\u0022\\u003Esample-boat-400x300.png\\u003C/item\\u003E\\u003C/param\\u003E\\u003Cparam name=\\u0022answerDeviation\\u0022\\u003E0.05\\u003C/param\\u003E\\u003C/params\\u003E\\u003Cright\\u003E\\u003Canswer\\u003E0.46,0.7\\u003C/answer\\u003E\\u003C/right\\u003E\\u003C/question\\u003E\",\"Authors\":[],\"Sources\":[],\"Images\":{\"sample-boat-400x300.png\":\"C:\\\\Users\\\\Vladimir\\\\AppData\\\\Local\\\\Temp\\\\SIQuester\\\\Media\\\\3080206585Gue47iwYsaPILmamDv9Ow.png\"},\"Audio\":{},\"Video\":{},\"Html\":{}}";

        // Act
        var data = JsonSerializer.Deserialize<InfoOwnerData>(json);

        // Assert
        Assert.That(data, Is.Not.Null);
        Assert.That(data!.ItemLevel, Is.EqualTo(InfoOwnerData.Level.Question));
        Assert.That(data.Images, Contains.Key("sample-boat-400x300.png"));
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

        // Act - add via the ViewModel collection (which syncs to Model)
        var newRound = new Round { Name = "New Round" };
        qDocument.Package.Rounds.Add(new RoundViewModel(newRound));

        // Assert
        Assert.That(qDocument.Package.Rounds.Count, Is.EqualTo(initialCount + 1));
        Assert.That(qDocument.Package.Rounds[^1].Model.Name, Is.EqualTo("New Round"));
    }

    [Test]
    public void AddThemeToRound_ShouldIncreaseThemeCount()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var round = qDocument.Package.Rounds[0];
        var initialCount = round.Themes.Count;

        // Act - add via the ViewModel collection (which syncs to Model)
        var newTheme = new Theme { Name = "New Theme" };
        round.Themes.Add(new ThemeViewModel(newTheme));

        // Assert
        Assert.That(round.Themes.Count, Is.EqualTo(initialCount + 1));
        Assert.That(round.Themes[^1].Model.Name, Is.EqualTo("New Theme"));
    }

    [Test]
    public void AddQuestionToTheme_ShouldIncreaseQuestionCount()
    {
        // Arrange
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var theme = qDocument.Package.Rounds[0].Themes[0];
        var initialCount = theme.Questions.Count;

        // Act - add via the ViewModel collection (which syncs to Model)
        var newQuestion = new Question { Price = 300 };
        newQuestion.Right.Add("New answer");
        theme.Questions.Add(new QuestionViewModel(newQuestion));

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

        // Act - remove via the ViewModel collection (which syncs to Model)
        qDocument.Package.Rounds.RemoveAt(0);

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

        // Act - remove via the ViewModel collection (which syncs to Model)
        round.Themes.RemoveAt(0);

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

        // Act - remove via the ViewModel collection (which syncs to Model)
        theme.Questions.RemoveAt(0);

        // Assert
        Assert.That(theme.Questions.Count, Is.EqualTo(initialCount - 1));
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

    #region Document Lifecycle

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
