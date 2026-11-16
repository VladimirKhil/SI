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
/// Tests the <see cref="StepParameterValues.AskAnswerCompletionMode_FirstCorrect" /> completion mode of a question for all
/// (<see cref="QuestionParameterNames.CompletionMode" />) on a real game engine:
/// all players answer simultaneously, but only the first right answer is scored.
/// </summary>
[TestFixture]
public sealed class FirstCorrectCompletionModeTests
{
    private const string HostName = "Showman";
    private const int Price = 10;
    private const string FirstCorrect = StepParameterValues.AskAnswerCompletionMode_FirstCorrect;

    /// <summary>
    /// Answering time long enough to distinguish an early finish of the answering stage from its timeout.
    /// </summary>
    private const int LongAnswerTime = 120;

    /// <summary>
    /// Time to wait for the answering stage to finish early (much less than <see cref="LongAnswerTime" />).
    /// </summary>
    private static readonly TimeSpan EarlyFinishTimeout = TimeSpan.FromSeconds(30);

    [Test]
    public async Task FirstCorrect_FirstRightTextAnswerWins_LaterAnswerIsNotJudged()
    {
        var (showman, playerA, playerB, _) = await StartQuestionAsync(FirstCorrect);

        playerA.Client.SendMessage(new MessageBuilder(Messages.Answer, "right").ToString(), receiver: NetworkConstants.GameName);
        var firstAskValidate = await showman.WaitForMessageAsync(Messages.AskValidate);
        Assert.That(firstAskValidate[1], Is.EqualTo("0"), "Player A answer should be asked for validation first");

        playerB.Client.SendMessage(new MessageBuilder(Messages.Answer, "another").ToString(), receiver: NetworkConstants.GameName);
        var secondAskValidate = await showman.WaitForMessageAsync(Messages.AskValidate);
        Assert.That(secondAskValidate[1], Is.EqualTo("1"), "Player B answer should be asked for validation second");

        // Player A wins, so player B answer does not need a verdict
        showman.Client.SendMessage(new MessageBuilder(Messages.Validate, "right", "+", 1).ToString(), receiver: NetworkConstants.GameName);

        var winner = await showman.WaitForMessageAsync(Messages.Person);
        Assert.That(winner[1], Is.EqualTo("+"), "Player A answer should be marked right");
        Assert.That(winner[2], Is.EqualTo("0"), "Player A should win");
        Assert.That(winner[3], Is.EqualTo(Price.ToString()), "Player A should get the price");

        var lateAnswer = await showman.WaitForMessageAsync(Messages.Person, Messages.Pass);
        Assert.That(lateAnswer[0], Is.EqualTo(Messages.Pass), "Player B answer given after the winner one should not be judged");
        Assert.That(lateAnswer[1], Is.EqualTo("1"));
    }

    [Test]
    public async Task FirstCorrect_WrongAnswerBeforeWinner_IsPenalized()
    {
        var (showman, playerA, playerB, _) = await StartQuestionAsync(FirstCorrect);

        playerA.Client.SendMessage(new MessageBuilder(Messages.Answer, "wrong").ToString(), receiver: NetworkConstants.GameName);
        await showman.WaitForMessageAsync(Messages.AskValidate);

        playerB.Client.SendMessage(new MessageBuilder(Messages.Answer, "right").ToString(), receiver: NetworkConstants.GameName);
        await showman.WaitForMessageAsync(Messages.AskValidate);

        showman.Client.SendMessage(new MessageBuilder(Messages.Validate, "wrong", "-", 1).ToString(), receiver: NetworkConstants.GameName);
        showman.Client.SendMessage(new MessageBuilder(Messages.Validate, "right", "+", 1).ToString(), receiver: NetworkConstants.GameName);

        var wrongAnswer = await showman.WaitForMessageAsync(Messages.Person);
        Assert.That(wrongAnswer[1], Is.EqualTo("-"), "Player A answer should be marked wrong");
        Assert.That(wrongAnswer[2], Is.EqualTo("0"));
        Assert.That(wrongAnswer[3], Is.EqualTo(Price.ToString()), "Player A should lose the price by the question for all rule");

        var winner = await showman.WaitForMessageAsync(Messages.Person);
        Assert.That(winner[1], Is.EqualTo("+"), "Player B answer should be marked right");
        Assert.That(winner[2], Is.EqualTo("1"), "Player B should win");
        Assert.That(winner[3], Is.EqualTo(Price.ToString()), "Player B should get the price");
    }

    [Test]
    public async Task FirstCorrect_AllPlayersPass_StageFinishesEarly()
    {
        var (showman, playerA, playerB, answerRequest) = await StartQuestionAsync(FirstCorrect, answerTime: LongAnswerTime);
        Assert.That(answerRequest, Is.EqualTo(new[] { Messages.Answer, "text", "+" }), "Players should be allowed to pass");

        playerA.Client.SendMessage(Messages.Pass, receiver: NetworkConstants.GameName);
        playerB.Client.SendMessage(Messages.Pass, receiver: NetworkConstants.GameName);

        var firstPass = await showman.WaitForMessageAsync(Messages.Pass);
        var secondPass = await showman.WaitForMessageAsync(Messages.Pass);
        Assert.That(new[] { firstPass[1], secondPass[1] }, Is.EquivalentTo(new[] { "0", "1" }), "Everybody should be informed about both passes");

        // Nobody has answered, so no score changes, and the stage does not wait for the timeout
        var next = await showman.WaitForMessageAsync(EarlyFinishTimeout, Messages.Person, Messages.RightAnswer);
        Assert.That(next[0], Is.EqualTo(Messages.RightAnswer));
    }

    [Test]
    public async Task FirstCorrect_MiniGame_FirstRightAnswerFinishesStage()
    {
        var (showman, _, playerB, answerRequest) = await StartQuestionAsync(
            FirstCorrect,
            answerType: StepParameterValues.SetAnswerTypeType_ManagedByClient,
            answerTime: LongAnswerTime);

        Assert.That(answerRequest, Is.EqualTo(new[] { Messages.Answer, "client", "+" }), "Players should be allowed to pass");

        // Player B completes the mini-game while player A is still playing
        playerB.Client.SendMessage(new MessageBuilder(Messages.Answer, MessageParams.Answer_Right).ToString(), receiver: NetworkConstants.GameName);

        var winner = await showman.WaitForMessageAsync(EarlyFinishTimeout, Messages.Person);
        Assert.That(winner[1], Is.EqualTo("+"));
        Assert.That(winner[2], Is.EqualTo("1"), "Player B should win");
        Assert.That(winner[3], Is.EqualTo(Price.ToString()), "Player B should get the price");

        var notAnswered = await showman.WaitForMessageAsync(Messages.Person, Messages.Pass);
        Assert.That(notAnswered[0], Is.EqualTo(Messages.Pass), "Player A should not lose anything");
        Assert.That(notAnswered[1], Is.EqualTo("0"));
    }

    [Test]
    public async Task DefaultCompletionMode_PassIsIgnored()
    {
        var (showman, playerA, _, answerRequest) = await StartQuestionAsync(completionMode: null);
        Assert.That(answerRequest, Is.EqualTo(new[] { Messages.Answer, "text" }), "Players should not be offered to pass");

        playerA.Client.SendMessage(Messages.Pass, receiver: NetworkConstants.GameName);
        playerA.Client.SendMessage(new MessageBuilder(Messages.Answer, "myAnswer").ToString(), receiver: NetworkConstants.GameName);

        var next = await showman.WaitForMessageAsync(Messages.Pass, Messages.AskValidate);
        Assert.That(next[0], Is.EqualTo(Messages.AskValidate), "The pass should be ignored and the answer should be accepted");
        Assert.That(next[1], Is.EqualTo("0"));
        Assert.That(next[2], Is.EqualTo("myAnswer"));
    }

    /// <summary>
    /// Starts a game with a showman and two players and plays a question for all up to the answering stage.
    /// </summary>
    /// <param name="completionMode">Answering stage completion mode (null if not specified).</param>
    /// <param name="answerType">Question answer type (text by default).</param>
    /// <param name="answerTime">Answering time in seconds.</param>
    /// <returns>Showman and players listeners and the answer request received by player A.</returns>
    private static async Task<(Listener Showman, Listener PlayerA, Listener PlayerB, string[] AnswerRequest)> StartQuestionAsync(
        string completionMode,
        string answerType = null,
        int answerTime = SI.Contracts.TimeSettings.DefaultHiddenAnswering)
    {
        var node = new PrimaryNode(new Network.Configuration.NodeConfiguration());

        var document = SIDocument.Create("Test Package", "Test Author");
        var round = new Round { Name = "Round 1", Type = RoundTypes.Standart };
        document.Package.Rounds.Add(round);
        var theme = new Theme { Name = "Test Theme" };
        round.Themes.Add(theme);
        var question = new Question { Price = Price, TypeName = QuestionTypes.ForAll };
        theme.Questions.Add(question);

        question.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue =
            [
                new() { Value = "Test question text", Type = ContentTypes.Text }
            ]
        };

        if (answerType != null)
        {
            question.Parameters[QuestionParameterNames.AnswerType] = new StepParameter
            {
                Type = StepParameterTypes.Simple,
                SimpleValue = answerType
            };
        }

        if (completionMode != null)
        {
            question.Parameters[QuestionParameterNames.CompletionMode] = new StepParameter
            {
                Type = StepParameterTypes.Simple,
                SimpleValue = completionMode
            };
        }

        question.Right.Add("right");

        var game = GameRunner.CreateGame(
            node,
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
            new SI.Contracts.TimeSettings { HiddenAnswering = answerTime },
            new RulesSettings(),
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

        showman.Client.SendMessage(Messages.Start);

        await showman.WaitForMessageAsync(Messages.AskSelectPlayer);
        showman.Client.SendMessage(new MessageBuilder(Messages.SelectPlayer, 0).ToString(), receiver: NetworkConstants.GameName);

        var answerRequest = await playerA.WaitForMessageAsync(Messages.Answer);
        await playerB.WaitForMessageAsync(Messages.Answer);

        return (showman, playerA, playerB, answerRequest);
    }

    private static Listener Join(Node node, Game game, string name, GameRole role)
    {
        var client = new Client(name);
        var listener = new Listener(client);
        client.ConnectTo(node);
        Assert.That(game.Authenticate(name, false, role, null), Is.EqualTo(AuthenticationResult.Ok));
        return listener;
    }

    private sealed class Listener
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

        private readonly Channel<Message> _messageChannel = Channel.CreateUnbounded<Message>();

        public Client Client { get; }

        public Listener(Client client)
        {
            Client = client;
            Client.MessageReceived += message => _messageChannel.Writer.WriteAsync(message);
        }

        /// <summary>
        /// Waits for the first message of any of the provided types.
        /// </summary>
        public Task<string[]> WaitForMessageAsync(params string[] messageTypes) => WaitForMessageAsync(DefaultTimeout, messageTypes);

        /// <summary>
        /// Waits for the first message of any of the provided types during the provided time.
        /// </summary>
        public async Task<string[]> WaitForMessageAsync(TimeSpan timeout, params string[] messageTypes)
        {
            using var cts = new CancellationTokenSource(timeout);

            try
            {
                while (await _messageChannel.Reader.WaitToReadAsync(cts.Token))
                {
                    while (_messageChannel.Reader.TryRead(out var message))
                    {
                        var args = message.Text.Split(Message.ArgsSeparatorChar, StringSplitOptions.RemoveEmptyEntries);

                        if (Array.IndexOf(messageTypes, args[0]) >= 0)
                        {
                            return args;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Reported below
            }

            throw new AssertionException($"Timeout waiting for {string.Join(", ", messageTypes)}");
        }
    }
}
