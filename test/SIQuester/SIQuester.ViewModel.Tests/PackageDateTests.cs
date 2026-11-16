using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;
using System.Globalization;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for the package creation date.
/// </summary>
[TestFixture]
internal sealed class PackageDateTests
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
    public void SetDateToday_ShouldSetTodayInPackageDateFormat()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        qDocument.Package.Model.Date = "01.01.2000";

        // Act
        qDocument.Package.SetDateToday.Execute(null);

        // Assert
        Assert.That(qDocument.Package.Model.Date, Is.EqualTo(DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)));
    }

    [Test]
    public void SetDateToday_Undo_ShouldRestorePreviousDate()
    {
        // Arrange
        var document = SIDocument.Create("Test Package", "Test Author");
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        qDocument.Package.Model.Date = "01.01.2000";

        // Act
        qDocument.Package.SetDateToday.Execute(null);
        qDocument.OperationsManager.Undo.Execute(null);

        // Assert
        Assert.That(qDocument.Package.Model.Date, Is.EqualTo("01.01.2000"));
    }
}
