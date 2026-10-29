using NSubstitute;
using NUnit.Framework;
using SI.Contracts;
using SICore.Contracts;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SIData;
using SIPackages;
using SIPackages.Core;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SICore.Tests.Scenarios;

/// <summary>
/// Tests <see cref="RulesSettings.HideImageOnAnswerAndPause" /> rule storage and broadcasting on a real game engine.
/// </summary>
[TestFixture]
public sealed class HideImageOnAnswerAndPauseTests
{
    private const string HostName = "Showman";
    private const string OptionName = nameof(RulesSettings.HideImageOnAnswerAndPause);

    [Test]
    public async Task Rule_IsDisabledByDefault_AndSentOnJoin()
    {
        var rules = new RulesSettings();
        var (_, showman, playerA, playerB) = StartGame(rules);

        Assert.That(rules.HideImageOnAnswerAndPause, Is.False);

        foreach (var listener in new[] { showman, playerA, playerB })
        {
            var options = await listener.WaitForOptions2Async();
            TestContext.Out.WriteLine($"{listener.Name} join OPTIONS2: {Escape(options.Text)}");

            Assert.That(options.Args[1], Is.Empty, "Join OPTIONS2 has no reason");
            Assert.That(GetOptionValue(options.Args), Is.EqualTo("False"));
        }
    }

    [Test]
    public async Task Rule_ChangedByHost_IsBroadcastToEveryone()
    {
        var rules = new RulesSettings();
        var (_, showman, playerA, playerB) = StartGame(rules);

        foreach (var listener in new[] { showman, playerA, playerB })
        {
            await listener.WaitForOptions2Async(); // Join settings
        }

        showman.Client.SendMessage(
            new MessageBuilder(Messages.SetOptions, OptionName, "true").ToString(),
            receiver: NetworkConstants.GameName);

        foreach (var listener in new[] { showman, playerA, playerB })
        {
            var options = await listener.WaitForOptions2Async();
            TestContext.Out.WriteLine($"{listener.Name} change OPTIONS2: {Escape(options.Text)}");

            Assert.That(options.Text, Is.EqualTo($"{Messages.Options2}\n{HostName}\n{OptionName}\ntrue"));
        }

        Assert.That(rules.HideImageOnAnswerAndPause, Is.True);

        // Settings for a newly informed person reflect the new value
        playerA.Client.SendMessage(Messages.Info, receiver: NetworkConstants.GameName);
        var info = await playerA.WaitForOptions2Async();
        TestContext.Out.WriteLine($"{playerA.Name} INFO OPTIONS2 after change: {Escape(info.Text)}");
        Assert.That(GetOptionValue(info.Args), Is.EqualTo("True"));
    }

    [Test]
    public async Task Rule_ChangedByNonHost_IsIgnored()
    {
        var rules = new RulesSettings();
        var (_, _, playerA, _) = StartGame(rules);

        await playerA.WaitForOptions2Async(); // Join settings

        playerA.Client.SendMessage(
            new MessageBuilder(Messages.SetOptions, OptionName, "true").ToString(),
            receiver: NetworkConstants.GameName);

        // Messages from the same sender are processed in order, so INFO response comes right after SET_OPTIONS handling
        playerA.Client.SendMessage(Messages.Info, receiver: NetworkConstants.GameName);

        var options = await playerA.WaitForOptions2Async();
        Assert.That(options.Args[1], Is.Empty, "No OPTIONS2 change broadcast is expected for non-host");
        Assert.That(GetOptionValue(options.Args), Is.EqualTo("False"));
        Assert.That(rules.HideImageOnAnswerAndPause, Is.False);
    }

    [Test]
    public async Task Rule_EnabledOnCreation_IsSentOnJoin()
    {
        var rules = new RulesSettings { HideImageOnAnswerAndPause = true };
        var (_, showman, playerA, playerB) = StartGame(rules);

        foreach (var listener in new[] { showman, playerA, playerB })
        {
            var options = await listener.WaitForOptions2Async();
            TestContext.Out.WriteLine($"{listener.Name} join OPTIONS2 (enabled on creation): {Escape(options.Text)}");

            Assert.That(GetOptionValue(options.Args), Is.EqualTo("True"));
        }

        Assert.That(rules.HideImageOnAnswerAndPause, Is.True);
    }

    private static (Game Game, Listener Showman, Listener PlayerA, Listener PlayerB) StartGame(RulesSettings rules)
    {
        var node = new PrimaryNode(new Network.Configuration.NodeConfiguration());

        var gameSettings = new GameSettingsCore<AppSettingsCore>
        {
            Showman = new Account { IsHuman = true, Name = Constants.FreePlace },
            Players =
            [
                new Account { IsHuman = true, Name = Constants.FreePlace },
                new Account { IsHuman = true, Name = Constants.FreePlace }
            ],
        };

        var document = SIDocument.Create("Test Package", "Test Author");
        var round = new Round { Name = "Round 1", Type = RoundTypes.Standart };
        document.Package.Rounds.Add(round);
        var theme = new Theme { Name = "Test Theme" };
        round.Themes.Add(theme);
        var question = new Question { Price = 10 };
        theme.Questions.Add(question);
        question.Right.Add("right");

        var game = GameRunner.CreateGame(
            node,
            gameSettings,
            new RoomSettings
            {
                HostName = HostName,
                Showman = new SI.Contracts.Models.Account
                {
                    Type = SI.Contracts.Models.AccountType.Human,
                    Name = Constants.FreePlace
                },
                Players =
                [
                    new SI.Contracts.Models.Account { Type = SI.Contracts.Models.AccountType.Human, Name = Constants.FreePlace },
                    new SI.Contracts.Models.Account { Type = SI.Contracts.Models.AccountType.Human, Name = Constants.FreePlace }
                ]
            },
            new SI.Contracts.TimeSettings(),
            rules,
            "en-US",
            document,
            Substitute.For<IGameHost>(),
            Substitute.For<IFileShare>(),
            Array.Empty<ComputerAccount>(),
            Array.Empty<ComputerAccount>(),
            Array.Empty<ComputerAccount>(),
            Substitute.For<IAvatarHelper>(),
            null,
            null);

        game.Run();

        var showman = Join(node, game, HostName, GameRole.Showman);
        var playerA = Join(node, game, "A", GameRole.Player);
        var playerB = Join(node, game, "B", GameRole.Player);

        return (game, showman, playerA, playerB);
    }

    private static Listener Join(Node node, Game game, string name, GameRole role)
    {
        var client = new Client(name);
        var listener = new Listener(client);
        client.ConnectTo(node);
        Assert.That(game.Authenticate(name, false, role, null), Is.EqualTo(AuthenticationResult.Ok));

        // Joined client requests game info (as SIOnline does), which includes OPTIONS2 from InformSettings
        client.SendMessage(Messages.Info, receiver: NetworkConstants.GameName);
        return listener;
    }

    private static string GetOptionValue(string[] args)
    {
        for (var i = 2; i + 1 < args.Length; i += 2)
        {
            if (args[i] == OptionName)
            {
                return args[i + 1];
            }
        }

        throw new AssertionException($"{OptionName} not found in OPTIONS2");
    }

    private static string Escape(string text) => text.Replace("\n", "\\n");

    private sealed class Listener
    {
        private readonly Channel<Message> _messageChannel = Channel.CreateUnbounded<Message>();

        public Client Client { get; }

        public string Name => Client.Name;

        public Listener(Client client)
        {
            Client = client;
            Client.MessageReceived += message => _messageChannel.Writer.WriteAsync(message);
        }

        public async Task<(string Text, string[] Args)> WaitForOptions2Async()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            while (await _messageChannel.Reader.WaitToReadAsync(cts.Token))
            {
                while (_messageChannel.Reader.TryRead(out var message))
                {
                    var args = message.Text.Split(Message.ArgsSeparatorChar);

                    if (args[0] == Messages.Options2)
                    {
                        return (message.Text, args);
                    }
                }
            }

            throw new InvalidOperationException($"Timeout waiting for {Messages.Options2}");
        }
    }
}
