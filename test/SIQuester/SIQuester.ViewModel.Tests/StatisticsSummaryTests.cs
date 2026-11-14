using Microsoft.Extensions.DependencyInjection;
using SIPackages;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Tests.Helpers;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for the package totals in the statistics panel.
/// </summary>
[TestFixture]
internal sealed class StatisticsSummaryTests
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
    public async Task Totals_CountRoundsThemesAndQuestions()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        var round = new Round { Name = "Round 2" };
        var theme = new Theme { Name = "Theme 2" };
        theme.Questions.Add(new Question { Price = 100 });
        theme.Questions.Add(new Question { Price = 200 });
        round.Themes.Add(theme);
        round.Themes.Add(new Theme { Name = "Theme 3" });
        document.Package.Rounds.Add(round);

        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var statistics = new StatisticsViewModel(qDocument);

        await WaitForResultAsync(statistics);

        Assert.That(statistics.RoundCount, Is.EqualTo(2));
        Assert.That(statistics.ThemeCount, Is.EqualTo(3));
        Assert.That(statistics.QuestionCount, Is.EqualTo(3));
    }

    [Test]
    public async Task Details_ContainRoundsButNotTotals()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var statistics = new StatisticsViewModel(qDocument);

        await WaitForResultAsync(statistics);

        var totalsLine = statistics.Result.Split(Environment.NewLine)[0];

        Assert.That(statistics.Details, Does.Contain("Round 1"));
        Assert.That(statistics.Result, Does.Contain(statistics.Details));
        Assert.That(statistics.Details, Does.Not.Contain(totalsLine));
    }

    [Test]
    public async Task HasNoWarnings_IsFalseWhenTextHasIssues()
    {
        var document = TestHelper.CreateSimpleTestPackage();
        var round = new Round { Name = "Round 2" };
        var theme = new Theme { Name = "Theme 2" };
        theme.Questions.Add(new Question { Price = 100 }); // No question text and no answer
        round.Themes.Add(theme);
        document.Package.Rounds.Add(round);

        var qDocument = _documentFactory.CreateViewModelFor(document, "Test Package");
        var statistics = new StatisticsViewModel(qDocument);

        await WaitForResultAsync(statistics);

        Assert.That(statistics.Details, Does.Contain("Theme 2"));
        Assert.That(statistics.HasNoWarnings, Is.False);
    }

    // The check runs asynchronously (links are checked in the background)
    private static async Task WaitForResultAsync(StatisticsViewModel statistics)
    {
        for (var i = 0; i < 100 && statistics.Result.Length == 0; i++)
        {
            await Task.Delay(50);
        }

        Assert.That(statistics.Result, Is.Not.Empty, "Statistics were not computed");
    }
}
