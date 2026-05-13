using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;

namespace SIQuester.ViewModel.Tests;

[TestFixture]
internal sealed class ShowmanCommentsTests
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
    public void AddShowmanComments_ShouldCreateCommentsAndAllowEditing()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];

        question.AddShowmanComments.Execute(null);
        question.Info.ShowmanComments.Text = "Showman note";

        Assert.That(question.Info.ShowmanComments, Is.Not.Null);
        Assert.That(question.Info.ShowmanComments.HasValue, Is.True);
        Assert.That(question.Model.Info.ShowmanComments, Is.Not.Null);
        Assert.That(question.Model.Info.ShowmanComments!.Text, Is.EqualTo("Showman note"));
    }

    [Test]
    public void UndoAfterAddingShowmanComments_ShouldRemoveThemInOneStep()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];

        question.AddShowmanComments.Execute(null);

        Assert.That(question.Info.ShowmanComments.HasValue, Is.True);
        Assert.That(question.Model.Info.ShowmanComments, Is.Not.Null);
        Assert.That(qDocument.OperationsManager.Undo.CanBeExecuted, Is.True);

        qDocument.OperationsManager.Undo.Execute(null);

        Assert.That(question.Info.ShowmanComments.HasValue, Is.False);
        Assert.That(question.Info.ShowmanComments.Text, Is.EqualTo(string.Empty));
        Assert.That(question.Model.Info.ShowmanComments, Is.Null);
    }

    [Test]
    public void ClearShowmanComments_ShouldRemoveShowmanComments()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        document.Package.Rounds[0].Themes[0].Questions[0].Info.ShowmanComments = new Comments { Text = "Showman note" };
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];

        question.Info.ShowmanComments.Clear();

        Assert.That(question.Info.ShowmanComments, Is.Not.Null);
        Assert.That(question.Info.ShowmanComments.HasValue, Is.False);
        Assert.That(question.Info.ShowmanComments.Text, Is.EqualTo(string.Empty));
        Assert.That(question.Model.Info.ShowmanComments, Is.Null);
    }

    [Test]
    public void ShowmanComments_ShouldExistEvenWhenModelValueIsMissing()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        document.Package.Rounds[0].Themes[0].Questions[0].Info.ShowmanComments = null;
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];

        Assert.That(question.Info.ShowmanComments, Is.Not.Null);
        Assert.That(question.Info.ShowmanComments.HasValue, Is.False);
        Assert.That(question.Info.ShowmanComments.Text, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ClearShowmanComments_ShouldAddUndoOperation()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        document.Package.Rounds[0].Themes[0].Questions[0].Info.ShowmanComments = new Comments { Text = "Showman note" };
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var question = qDocument.Package.Rounds[0].Themes[0].Questions[0];

        question.Info.ShowmanComments.Clear();

        Assert.That(qDocument.OperationsManager.Undo.CanBeExecuted, Is.True);

        qDocument.OperationsManager.Undo.Execute(null);

        Assert.That(question.Info.ShowmanComments.HasValue, Is.True);
        Assert.That(question.Info.ShowmanComments.Text, Is.EqualTo("Showman note"));
        Assert.That(question.Model.Info.ShowmanComments, Is.Not.Null);
        Assert.That(question.Model.Info.ShowmanComments!.Text, Is.EqualTo("Showman note"));
    }
}
