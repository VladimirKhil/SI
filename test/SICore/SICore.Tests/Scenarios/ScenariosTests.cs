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
    /// <summary>
    /// Tests the complete game flow with ForAll question type.
    /// Validates the message sequence as documented in GAME_AGENT_DOCUMENTATION.md.
    /// </summary>
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

        var document = SIDocument.Create("Test Package", "Test Author");

        var round = new Round { Name = "Round 1", Type = RoundTypes.Standart };
        document.Package.Rounds.Add(round);

        var theme = new Theme { Name = "Test Theme" };
        round.Themes.Add(theme);

        var question = new Question { Price = 10, TypeName = QuestionTypes.ForAll };
        theme.Questions.Add(question);

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new() { Value = "Test question text", Type = ContentTypes.Text }
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
            null,
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
        var playerBListener = new MessageListener(playerBClient);
        playerBClient.ConnectTo(node);
        game.Join(playerBClient.Name, false, GameRole.Player, null, () => { });

        showmanClient.SendMessage(Messages.Start);

        // Game Initialization Phase - validate sequence
        await showmanListener.AssertNextMessageAsync(Messages.Stage);
        await showmanListener.AssertNextMessageAsync(Messages.PackageId);
        await showmanListener.AssertNextMessageAsync(Messages.RoundsNames);
        await showmanListener.AssertNextMessageAsync(Messages.PackageAuthors); // Sent before PACKAGE if authors exist
        await showmanListener.AssertNextMessageAsync(Messages.Package);
        await showmanListener.AssertNextMessageAsync(Messages.GameThemes);
        await showmanListener.AssertNextMessageAsync(Messages.Sums);

        // Round Start Phase
        await showmanListener.AssertNextMessageAsync(Messages.Stage);
        await showmanListener.AssertNextMessageAsync(Messages.RoundThemes);
        await showmanListener.AssertNextMessageAsync(Messages.RoundThemes2);
        await showmanListener.AssertNextMessageAsync(Messages.Theme2);
        await showmanListener.AssertNextMessageAsync(Messages.Table);
        await showmanListener.AssertNextMessageAsync(Messages.First);
        await showmanListener.AssertNextMessageAsync(Messages.AskSelectPlayer);

        // Question Selection Phase
        var msg = new MessageBuilder(Messages.SelectPlayer, 0);
        showmanClient.SendMessage(msg.ToString(), receiver: NetworkConstants.GameName);

        await showmanListener.AssertNextMessageAsync(Messages.SetChooser);
        await showmanListener.AssertNextMessageAsync(Messages.Sums);
        await showmanListener.AssertNextMessageAsync(Messages.ShowTable);
        await showmanListener.AssertNextMessageAsync(Messages.Choice);

        // Question Playback Phase - Start
        await showmanListener.AssertNextMessageAsync(Messages.QType);
        await showmanListener.AssertNextMessageAsync(Messages.Content);

        // Answer collection metadata for showman
        var complexValidationMessage = await showmanListener.AssertNextMessageAsync(Messages.QuestionAnswers);

        // Players receive answer request
        var answerMessage = await playerAListener.WaitForMessageAsync(Messages.Answer);

        // Player A submits answer
        playerAClient.SendMessage(new MessageBuilder(Messages.Answer, "myAnswer").ToString(), receiver: NetworkConstants.GameName);

        await showmanListener.AssertNextMessageAsync(Messages.PersonFinalAnswer);
        
        // Answer Validation Phase
        var askValidate = await showmanListener.AssertNextMessageAsync(Messages.AskValidate);
        Assert.That(askValidate[1], Is.EqualTo("0"), "Player index should be 0");
        Assert.That(askValidate[2], Is.EqualTo("myAnswer"), "Answer text should match");

        // Showman validates answer as correct
        var validateMsg = new MessageBuilder(Messages.Validate, "myAnswer", "+", 1);
        showmanClient.SendMessage(validateMsg.ToString(), receiver: NetworkConstants.GameName);

        await showmanListener.AssertNextMessageAsync(Messages.PlayerAnswer);

        // Validation result broadcast
        var personMsg = await showmanListener.AssertNextMessageAsync(Messages.Person);
        Assert.That(personMsg[1], Is.EqualTo("+"), "Answer should be marked correct");
        Assert.That(personMsg[2], Is.EqualTo("0"), "Player index should be 0");
        Assert.That(personMsg[3], Is.EqualTo("10"), "Score change should be 10");

        await showmanListener.AssertNextMessageAsync(Messages.Sums);
        
        // Second player answer processing
        await showmanListener.AssertNextMessageAsync(Messages.PlayerAnswer);
        await showmanListener.AssertNextMessageAsync(Messages.Person);
        await showmanListener.AssertNextMessageAsync(Messages.Sums);

        // Right Answer Display
        await showmanListener.AssertNextMessageAsync(Messages.RightAnswer);

        // Question End Phase
        await showmanListener.AssertNextMessageAsync(Messages.QuestionEnd);
        await showmanListener.AssertNextMessageAsync(Messages.Sums);

        // Round End Phase
        await showmanListener.AssertNextMessageAsync(Messages.Stop);

        // Game End Phase
        await showmanListener.AssertNextMessageAsync(Messages.Stop);
        await showmanListener.AssertNextMessageAsync(Messages.Winner);
        await showmanListener.AssertNextMessageAsync(Messages.Stage);
        await showmanListener.AssertNextMessageAsync(Messages.GameStatistics);
    }

    /// <summary>
    /// Tests game initialization sequence.
    /// Validates that initialization messages are sent in the correct order as documented.
    /// </summary>
    [Test]
    public async Task GameInitializationSequenceTest()
    {
        var node = new PrimaryNode(new Network.Configuration.NodeConfiguration());

        var gameSettings = new GameSettingsCore<AppSettingsCore>
        {
            Showman = new Account { IsHuman = true, Name = Constants.FreePlace },
            Players = new[]
            {
                new Account { IsHuman = true, Name = Constants.FreePlace }
            },
            HumanPlayerName = "Showman"
        };

        var document = SIDocument.Create("Test Package", "Test Author");
        var round = new Round { Name = "Round 1", Type = RoundTypes.Standart };
        document.Package.Rounds.Add(round);
        var theme = new Theme { Name = "Theme 1" };
        round.Themes.Add(theme);
        var question = new Question { Price = 100 };
        theme.Questions.Add(question);

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new() { Value = "Question", Type = ContentTypes.Text }
            }
        };
        question.Right.Add("answer");

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
            null,
            null);

        game.Run();

        var showmanClient = new Client("Showman");
        var showmanListener = new MessageListener(showmanClient);
        showmanClient.ConnectTo(node);
        game.Join(showmanClient.Name, false, GameRole.Showman, null, () => { });
        
        var playerClient = new Client("Player1");
        var playerListener = new MessageListener(playerClient);
        playerClient.ConnectTo(node);
        game.Join(playerClient.Name, false, GameRole.Player, null, () => { });

        showmanClient.SendMessage(Messages.Start);

        // Verify initialization sequence as per documentation
        var stage = await showmanListener.AssertNextMessageAsync(Messages.Stage);
        Assert.That(stage[1], Is.EqualTo("Begin"), "First stage should be Begin");

        await showmanListener.AssertNextMessageAsync(Messages.PackageId);
        
        var roundsNames = await showmanListener.AssertNextMessageAsync(Messages.RoundsNames);
        Assert.That(roundsNames.Length, Is.GreaterThan(1), "Should contain at least round name");

        await showmanListener.AssertNextMessageAsync(Messages.PackageAuthors); // Sent before PACKAGE if authors exist

        var package = await showmanListener.AssertNextMessageAsync(Messages.Package);
        Assert.That(package[1], Is.EqualTo("Test Package"), "Package name should match");

        var gameThemes = await showmanListener.AssertNextMessageAsync(Messages.GameThemes);
        Assert.That(gameThemes.Length, Is.GreaterThan(1), "Should contain theme names");

        var sums = await showmanListener.AssertNextMessageAsync(Messages.Sums);
        Assert.That(sums.Length, Is.EqualTo(2), "Should have sums for 1 player (SUMS + player sum)");
    }

    /// <summary>
    /// Tests round start message sequence.
    /// Validates that round messages are sent in correct order.
    /// </summary>
    [Test]
    public async Task RoundStartSequenceTest()
    {
        var node = new PrimaryNode(new Network.Configuration.NodeConfiguration());

        var gameSettings = new GameSettingsCore<AppSettingsCore>
        {
            Showman = new Account { IsHuman = true, Name = Constants.FreePlace },
            Players = new[]
            {
                new Account { IsHuman = true, Name = Constants.FreePlace }
            },
            HumanPlayerName = "Showman"
        };

        var document = SIDocument.Create("Package", "Author");
        var round = new Round { Name = "Test Round", Type = RoundTypes.Standart };
        document.Package.Rounds.Add(round);
        
        var theme1 = new Theme { Name = "Theme A" };
        var theme2 = new Theme { Name = "Theme B" };
        round.Themes.Add(theme1);
        round.Themes.Add(theme2);
        
        var question1 = new Question { Price = 100 };
        question1.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem> { new() { Value = "Q1", Type = ContentTypes.Text } }
        };
        question1.Right.Add("A1");
        theme1.Questions.Add(question1);

        var question2 = new Question { Price = 200 };
        question2.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem> { new() { Value = "Q2", Type = ContentTypes.Text } }
        };
        question2.Right.Add("A2");
        theme2.Questions.Add(question2);

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
            null,
            null);

        game.Run();

        var showmanClient = new Client("Showman");
        var showmanListener = new MessageListener(showmanClient);
        showmanClient.ConnectTo(node);
        game.Join(showmanClient.Name, false, GameRole.Showman, null, () => { });
        
        var playerClient = new Client("Player1");
        playerClient.ConnectTo(node);
        game.Join(playerClient.Name, false, GameRole.Player, null, () => { });

        showmanClient.SendMessage(Messages.Start);

        // Skip initialization
        await showmanListener.AssertNextMessageAsync(Messages.Stage); // Begin
        await showmanListener.AssertNextMessageAsync(Messages.PackageId);
        await showmanListener.AssertNextMessageAsync(Messages.RoundsNames);
        await showmanListener.AssertNextMessageAsync(Messages.PackageAuthors); // Sent before PACKAGE if authors exist
        await showmanListener.AssertNextMessageAsync(Messages.Package);
        await showmanListener.AssertNextMessageAsync(Messages.GameThemes);
        await showmanListener.AssertNextMessageAsync(Messages.Sums);

        // Verify round start sequence
        var roundStage = await showmanListener.AssertNextMessageAsync(Messages.Stage);
        // Note: The stage message sometimes just says "Round" not the full round name
        Assert.That(roundStage.Length, Is.GreaterThan(1), "Stage message should have round info");

        var roundThemes = await showmanListener.AssertNextMessageAsync(Messages.RoundThemes);
        Assert.That(roundThemes.Length, Is.EqualTo(3), "Should have 2 themes (message type + 2 themes)");

        await showmanListener.AssertNextMessageAsync(Messages.RoundThemes2);

        // Themes displayed sequentially
        var theme2Msg = await showmanListener.AssertNextMessageAsync(Messages.Theme2);
        Assert.That(theme2Msg[1], Is.EqualTo("Theme A"), "First theme should be Theme A");

        var theme2Msg2 = await showmanListener.AssertNextMessageAsync(Messages.Theme2);
        Assert.That(theme2Msg2[1], Is.EqualTo("Theme B"), "Second theme should be Theme B");

        var table = await showmanListener.AssertNextMessageAsync(Messages.Table);
        Assert.That(table.Length, Is.GreaterThan(3), "Table should contain theme and question info");
    }

    /// <summary>
    /// Tests button press question flow.
    /// Validates messages for questions where players press a button to answer.
    /// </summary>
    [Test]
    public async Task ButtonPressQuestionFlowTest()
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

        var document = SIDocument.Create("Package", "Author");
        var round = new Round { Name = "Round", Type = RoundTypes.Standart };
        document.Package.Rounds.Add(round);
        var theme = new Theme { Name = "Theme" };
        round.Themes.Add(theme);
        
        // Standard button press question
        var question = new Question { Price = 100, TypeName = QuestionTypes.Simple };
        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new() { Value = "Press button question", Type = ContentTypes.Text }
            }
        };
        question.Right.Add("correct answer");
        theme.Questions.Add(question);

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
            null,
            null);

        game.Run();

        var showmanClient = new Client("Showman");
        var showmanListener = new MessageListener(showmanClient);
        showmanClient.ConnectTo(node);
        game.Join(showmanClient.Name, false, GameRole.Showman, null, () => { });
        
        var playerAClient = new Client("PlayerA");
        var playerAListener = new MessageListener(playerAClient);
        playerAClient.ConnectTo(node);
        game.Join(playerAClient.Name, false, GameRole.Player, null, () => { });
        
        var playerBClient = new Client("PlayerB");
        var playerBListener = new MessageListener(playerBClient);
        playerBClient.ConnectTo(node);
        game.Join(playerBClient.Name, false, GameRole.Player, null, () => { });

        showmanClient.SendMessage(Messages.Start);

        // Skip to question selection
        await showmanListener.AssertNextMessageAsync(Messages.Stage);
        await showmanListener.AssertNextMessageAsync(Messages.PackageId);
        await showmanListener.AssertNextMessageAsync(Messages.RoundsNames);
        await showmanListener.AssertNextMessageAsync(Messages.PackageAuthors); // Sent before PACKAGE if authors exist
        await showmanListener.AssertNextMessageAsync(Messages.Package);
        await showmanListener.AssertNextMessageAsync(Messages.GameThemes);
        await showmanListener.AssertNextMessageAsync(Messages.Sums);
        await showmanListener.AssertNextMessageAsync(Messages.Stage);
        await showmanListener.AssertNextMessageAsync(Messages.RoundThemes);
        await showmanListener.AssertNextMessageAsync(Messages.RoundThemes2);
        await showmanListener.AssertNextMessageAsync(Messages.Theme2);
        await showmanListener.AssertNextMessageAsync(Messages.Table);
        
        // Player selection messages - order may vary
        var nextMsg = await showmanListener.AssertNextMessageAsync(Messages.ShowTable);
        await showmanListener.AssertNextMessageAsync(Messages.First);
        await showmanListener.AssertNextMessageAsync(Messages.AskSelectPlayer);

        // Select question
        showmanClient.SendMessage(new MessageBuilder(Messages.SelectPlayer, 0).ToString(), receiver: NetworkConstants.GameName);
        await showmanListener.AssertNextMessageAsync(Messages.SetChooser);
        await showmanListener.AssertNextMessageAsync(Messages.Sums);
        await showmanListener.AssertNextMessageAsync(Messages.ShowTable);
        await showmanListener.AssertNextMessageAsync(Messages.Choice);

        // Question playback - should have QUESTION message
        var questionMsg = await showmanListener.AssertNextMessageAsync(Messages.Question);
        Assert.That(questionMsg[1], Is.EqualTo("100"), "Question price should be 100");

        // Question type
        var qtypeMsg = await showmanListener.AssertNextMessageAsync(Messages.QType);
        Assert.That(qtypeMsg[1], Is.EqualTo("simple"), "Question type should be simple");

        // Content display
        await showmanListener.AssertNextMessageAsync(Messages.ContentShape);
        var contentMsg = await showmanListener.AssertNextMessageAsync(Messages.Content);

        // Button press phase - TRY message enables buttons
        await showmanListener.AssertNextMessageAsync(Messages.Try);

        // Player A presses button
        playerAClient.SendMessage(Messages.I, receiver: NetworkConstants.GameName);

        // Player A should receive YOUTRY confirmation
        var youTryMsg = await playerAListener.WaitForMessageAsync(Messages.YouTry);
        Assert.That(youTryMsg, Is.Not.Null, "Player should receive YOUTRY message");

        // All players should receive ENDTRY to disable buttons
        await showmanListener.AssertNextMessageAsync(Messages.EndTry);

        // Player A gets ANSWER request
        var answerReq = await playerAListener.WaitForMessageAsync(Messages.Answer);
        Assert.That(answerReq, Is.Not.Null, "Player should receive ANSWER request");

        // Player A submits answer
        playerAClient.SendMessage(new MessageBuilder(Messages.Answer, "correct answer").ToString(), receiver: NetworkConstants.GameName);

        // Answer should be displayed
        await showmanListener.AssertNextMessageAsync(Messages.PlayerAnswer);

        // Showman receives validation request
        var validateReq = await showmanListener.AssertNextMessageAsync(Messages.AskValidate);
        Assert.That(validateReq[1], Is.EqualTo("0"), "Player index should be 0");
        Assert.That(validateReq[2], Is.EqualTo("correct answer"), "Answer should match");

        // Showman validates as correct
        showmanClient.SendMessage(new MessageBuilder(Messages.Validate, "correct answer", "+", 1).ToString(), receiver: NetworkConstants.GameName);

        // Validation result broadcast
        var personMsg = await showmanListener.AssertNextMessageAsync(Messages.Person);
        Assert.That(personMsg[1], Is.EqualTo("+"), "Should be marked correct");
        Assert.That(personMsg[2], Is.EqualTo("0"), "Player index should be 0");

        // Scores updated
        await showmanListener.AssertNextMessageAsync(Messages.Sums);

        // Right answer shown
        await showmanListener.AssertNextMessageAsync(Messages.RightAnswerStart);
        await showmanListener.AssertNextMessageAsync(Messages.RightAnswer);

        // Question ends
        await showmanListener.AssertNextMessageAsync(Messages.QuestionEnd);
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
            Messages.ShowmanReplic,
            Messages.Timer,
            Messages.PlayerState,
            // Optional package metadata that may be sent
            Messages.PackageSources,
            Messages.PackageDate,
            Messages.PackageComments,
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
