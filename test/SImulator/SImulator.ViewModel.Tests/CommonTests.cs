using NUnit.Framework;
using SImulator.ViewModel.Controllers;
using SImulator.ViewModel.Model;

namespace SImulator.ViewModel.Tests;

public sealed class CommonTests
{
    private readonly TestPlatformManager _manager = new();

    [SetUp]
    public void Setup()
    {
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
        Assert.NotNull(game);

        game.Next.Execute(null);
        game.Next.Execute(null);
        game.Next.Execute(null);
        game.Next.Execute(null);
        game.Next.Execute(null);

        game.LocalInfo.SelectQuestion.Execute(game.LocalInfo.RoundInfo[0].Questions[0]);

        game.Next.Execute(null);

        Assert.AreEqual("В этой передаче гроссмейстеры «Своей игры» сражались с приглашёнными знаменитостями",
            ((PresentationController)game.PresentationController).TInfo.Text);
    }
}