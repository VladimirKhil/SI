using Microsoft.Extensions.DependencyInjection;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for removing all authors / sources at once (the "×" button).
/// </summary>
[TestFixture]
internal sealed class ClearLinksTests
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

    [Test]
    public void ClearAll_RemovesAllQuestionAuthors_AndOneUndoRestoresThem()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        var questionModel = document.Package.Rounds[0].Themes[0].Questions[0];
        questionModel.Info.Authors.Add("Author 1");
        questionModel.Info.Authors.Add("Author 2");

        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var authors = qDocument.Package.Rounds[0].Themes[0].Questions[0].Info.Authors;

        Assert.That(authors.ClearAll.CanBeExecuted, Is.True);

        authors.ClearAll.Execute(null);

        Assert.That(authors, Is.Empty);
        Assert.That(questionModel.Info.Authors, Is.Empty);

        qDocument.OperationsManager.Undo.Execute(null);

        Assert.That(authors, Is.EqualTo(new[] { "Author 1", "Author 2" }));
        Assert.That(questionModel.Info.Authors, Is.EqualTo(new[] { "Author 1", "Author 2" }));
        Assert.That(qDocument.OperationsManager.Undo.CanBeExecuted, Is.False);
    }

    [Test]
    public void ClearAll_RemovesAllThemeSources_AndOneUndoRestoresThem()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        var themeModel = document.Package.Rounds[0].Themes[0];
        themeModel.Info.Sources.Add("Source 1");
        themeModel.Info.Sources.Add("Source 2");
        themeModel.Info.Sources.Add("Source 3");

        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var sources = qDocument.Package.Rounds[0].Themes[0].Info.Sources;

        sources.ClearAll.Execute(null);

        Assert.That(sources, Is.Empty);
        Assert.That(themeModel.Info.Sources, Is.Empty);

        qDocument.OperationsManager.Undo.Execute(null);

        Assert.That(themeModel.Info.Sources, Is.EqualTo(new[] { "Source 1", "Source 2", "Source 3" }));
        Assert.That(qDocument.OperationsManager.Undo.CanBeExecuted, Is.False);
    }

    [Test]
    public void ClearAll_KeepsPackageAuthors()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var authors = qDocument.Package.Info.Authors;

        Assert.That(authors.ClearAll.CanBeExecuted, Is.False);

        authors.ClearAll.Execute(null);

        Assert.That(authors, Is.EqualTo(new[] { "Test Author" }));
        Assert.That(document.Package.Info.Authors, Is.EqualTo(new[] { "Test Author" }));
        Assert.That(qDocument.OperationsManager.Undo.CanBeExecuted, Is.False);
    }

    [Test]
    public void ClearAll_OnEmptyList_DoesNotAddUndoStep()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var authors = qDocument.Package.Rounds[0].Info.Authors;

        authors.ClearAll.Execute(null);

        Assert.That(qDocument.OperationsManager.Undo.CanBeExecuted, Is.False);
    }
}
