using NSubstitute;
using NUnit.Framework;
using SICore.Contracts;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SICore.Utils;
using SIData;
using SIPackages;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SICore.Tests.Scenarios;

[TestFixture]
public sealed class ScenariosTests
{
    [Test]
    public async Task MainTest()
    {
        var node = new PrimaryNode(new Network.Configuration.NodeConfiguration());

        var gameSettings = new GameSettingsCore<AppSettingsCore>
        {
            Showman = new Account { IsHuman = true, Name = Constants.FreePlace },
            Players = new[]
            {
                new Account { IsHuman = true, Name = Constants.FreePlace },
                new Account { IsHuman = true, Name = Constants.FreePlace }
            },
            HumanPlayerName = "Showman"
        };

        var timeSettings = gameSettings.AppSettings.TimeSettings;

        var document = SIDocument.Create("", "");

        var round = new Round { Name = "round", Type = RoundTypes.Standart };
        document.Package.Rounds.Add(round);

        var theme = new Theme { Name = "theme" };
        round.Themes.Add(theme);

        var question = new Question { Price = 10, TypeName = QuestionTypes.ForAll };
        theme.Questions.Add(question);

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new() { Value = "Text", Type = ContentTypes.Text }
            }
        };

        question.Right.Add("right");

        var gameHost = Substitute.For<IGameHost>();
        var fileShare = Substitute.For<IFileShare>();
        var avatarHelper = Substitute.For<IAvatarHelper>();

        var game = GameRunner.CreateGame(
            node,
            gameSettings,
            document,
            gameHost,
            fileShare,
            Array.Empty<ComputerAccount>(),
            Array.Empty<ComputerAccount>(),
            avatarHelper,
            null);

        game.Run();

        var showmanClient = new Client("Showman");
        var showmanListener = new MessageListener(showmanClient);
        showmanClient.ConnectTo(node);
        var (res, _) = game.Join(showmanClient.Name, false, GameRole.Showman, null, () => { });
        Assert.That(res, Is.True);
        
        var playerAClient = new Client("A");
        var playerAListener = new MessageListener(playerAClient);
        playerAClient.ConnectTo(node);
        game.Join(playerAClient.Name, false, GameRole.Player, null, () => { });
        
        var playerBClient = new Client("B");
        playerBClient.ConnectTo(node);
        game.Join(playerBClient.Name, false, GameRole.Player, null, () => { });

        showmanClient.SendMessage(Messages.Start);

        await showmanListener.AssertNextMessageAsync(Messages.Stage);
        await showmanListener.AssertNextMessageAsync(Messages.PackageId);
        await showmanListener.AssertNextMessageAsync(Messages.RoundsNames);
        await showmanListener.AssertNextMessageAsync(Messages.Package);
        await showmanListener.AssertNextMessageAsync(Messages.GameThemes);
        await showmanListener.AssertNextMessageAsync(Messages.Sums);
        await showmanListener.AssertNextMessageAsync(Messages.Stage);
        await showmanListener.AssertNextMessageAsync(Messages.RoundThemes);
        await showmanListener.AssertNextMessageAsync(Messages.RoundThemes2);
        await showmanListener.AssertNextMessageAsync(Messages.Table);
        await showmanListener.AssertNextMessageAsync(Messages.First);
        await showmanListener.AssertNextMessageAsync(Messages.AskSelectPlayer);

        var msg = new MessageBuilder(Messages.SelectPlayer, 0);
        showmanClient.SendMessage(msg.ToString(), receiver: NetworkConstants.GameName);

        await showmanListener.AssertNextMessageAsync(Messages.SetChooser);
        await showmanListener.AssertNextMessageAsync(Messages.Sums);
        await showmanListener.AssertNextMessageAsync(Messages.ShowTable);
        await showmanListener.AssertNextMessageAsync(Messages.Choice);
        await showmanListener.AssertNextMessageAsync(Messages.QType);

        await showmanListener.AssertNextMessageAsync(Messages.Content);
        var complexValidationMessage = await showmanListener.AssertNextMessageAsync(Messages.QuestionAnswers);

        var answerMessage = await playerAListener.WaitForMessageAsync(Messages.Answer);

        playerAClient.SendMessage(new MessageBuilder(Messages.Answer, "myAnswer").ToString(), receiver: NetworkConstants.GameName);

        await showmanListener.AssertNextMessageAsync(Messages.PersonFinalAnswer);
        
        var askValidate = await showmanListener.AssertNextMessageAsync(Messages.AskValidate);
        Assert.That(askValidate[1], Is.EqualTo("0"));
        Assert.That(askValidate[2], Is.EqualTo("myAnswer"));

        var validateMsg = new MessageBuilder(Messages.Validate, 0, "+", 1);
        showmanClient.SendMessage(validateMsg.ToString(), receiver: NetworkConstants.GameName);

        await showmanListener.AssertNextMessageAsync(Messages.Cancel);
        var personMsg = await showmanListener.AssertNextMessageAsync(Messages.Person);
        Assert.That(personMsg[1], Is.EqualTo("+"));
        Assert.That(personMsg[2], Is.EqualTo("0"));
        Assert.That(personMsg[3], Is.EqualTo("10"));

        await showmanListener.AssertNextMessageAsync(Messages.Sums);
    }

    private sealed class MessageListener : IDisposable
    {
        private readonly Client _client;
        private readonly Channel<Message> _messageChannel = Channel.CreateUnbounded<Message>();

        private static readonly HashSet<string> _messagesToSkip = new(new[]
        {
            Messages.Connect,
            Messages.Connected,
            Messages.Replic,
            Messages.Timer,
            Messages.PlayerState,
        });

        public MessageListener(Client client)
        {
            _client = client;
            _client.MessageReceived += Client_MessageReceived;
        }

        public async Task<string[]> AssertNextMessageAsync(string expectedMessageType, CancellationToken cancellationToken = default)
        {
            while (await _messageChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_messageChannel.Reader.TryRead(out var message))
                {
                    var messageArgs = message.Text.Split(Message.ArgsSeparator, StringSplitOptions.RemoveEmptyEntries);
                    var actualMessageType = messageArgs[0];

                    if (_messagesToSkip.Contains(actualMessageType))
                    {
                        continue;
                    }

                    Assert.That(actualMessageType, Is.EqualTo(expectedMessageType));
                    return messageArgs;
                }
            }

            throw new InvalidOperationException();
        }

        public async Task<string[]> WaitForMessageAsync(string messageType, CancellationToken cancellationToken = default)
        {
            while (await _messageChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_messageChannel.Reader.TryRead(out var message))
                {
                    var messageArgs = message.Text.Split(Message.ArgsSeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (messageArgs[0] == messageType)
                    {
                        return messageArgs;
                    }
                }
            }

            throw new InvalidOperationException();
        }

        private System.Threading.Tasks.ValueTask Client_MessageReceived(Message arg) => _messageChannel.Writer.WriteAsync(arg);

        public void Dispose()
        {
            _messageChannel.Writer.Complete();
            _client.MessageReceived -= Client_MessageReceived;
        }
    }
}
