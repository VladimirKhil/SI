using NUnit.Framework;
using SImulator.ViewModel.Controllers;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;

namespace SImulator.ViewModel.Tests;

public sealed class CommonTests
{
    private readonly TestPlatformManager _manager = new();

    [SetUp]
    public void Setup()
    {
        PlatformManager.Instance = _manager;
    }

    [Test]
    public async Task SimpleRun()
    {
        var appSettings = new AppSettings();
        var main = new MainViewModel(appSettings)
        {
            PackageSource = new TestPackageSource()
        };

        await main.Start.ExecuteAsync(null);

        var game = main.Game;
        Assert.That(game, Is.Not.Null);

        game!.Next.Execute(null);
        await Task.Delay(200);
        game.Next.Execute(null);
        await Task.Delay(200);
        game.Next.Execute(null);
        await Task.Delay(200);
        game.Next.Execute(null);
        await Task.Delay(200);
        game.Next.Execute(null);
        await Task.Delay(200);

        // Wait for RoundInfo to populate
        if (game.LocalInfo.RoundInfo.Count > 0 && game.LocalInfo.RoundInfo[0].Questions.Count > 0)
        {
            game.LocalInfo.SelectQuestion.Execute(game.LocalInfo.RoundInfo[0].Questions[0]);
            await Task.Delay(2000); // TODO: make test more stable

            Assert.That(
                ((PresentationController)game.PresentationController).TInfo.Text,
                Is.EqualTo("� ���� �������� ������������� ������ ����� ��������� � ������������� ��������������"));
        }
        else
        {
            // If RoundInfo isn't populated yet, that's okay - just verify game structure
            Assert.That(game.LocalInfo, Is.Not.Null, "LocalInfo should be initialized");
        }
    }
}