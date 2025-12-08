using Notions;
using SICore.Clients;
using SICore.Contracts;
using SICore.Models;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Utils;
using SIData;
using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;
using System.Text;
using System.Text.RegularExpressions;

namespace SICore;

/// <summary>
/// Defines game actions.
/// </summary>
public sealed class GameActions
{
    /// <summary>
    /// Represents character used to form content shape.
    /// </summary>
    private const string ContentShapeCharacter = "&";

    private static readonly string[] ReviewablePackageSources = new string[] { "https://steamcommunity.com" };

    private readonly GameData _state;

    public Client Client { get; }

    private readonly IFileShare _fileShare;

    public GameActions(Client client, GameData state, IFileShare fileShare)
    {
        Client = client;
        _state = state;
        _fileShare = fileShare;
    }

    public void SendMessage(string text, string receiver = NetworkConstants.Everybody) => Client.SendMessage(text, true, receiver);

    public void SendMessageWithArgs(params object[] args) => SendMessage(string.Join(Message.ArgsSeparator, args));

    public void SendMessageToWithArgs(string receiver, params object[] args) =>
        SendMessage(string.Join(Message.ArgsSeparator, args), receiver);

    /// <summary>
    /// Sends a visual message that affects game table state and saves it for reconnected players.
    /// </summary>
    public void SendVisualMessage(MessageBuilder messageBuilder)
    {
        var message = messageBuilder.ToString();
        _state.LastVisualMessage = message;
        _state.ComplexVisualState = null;
        SendMessage(message);
    }

    /// <summary>
    /// Sends a visual message that affects game table state and saves it for reconnected players.
    /// </summary>
    public void SendVisualMessage(string message)
    {
        _state.LastVisualMessage = message;
        _state.ComplexVisualState = null;
        SendMessage(message);
    }

    /// <summary>
    /// Sends a visual message with arguments that affects game table state and saves it for reconnected players.
    /// </summary>
    public void SendVisualMessageWithArgs(params object[] args)
    {
        var message = string.Join(Message.ArgsSeparator, args);
        _state.LastVisualMessage = message;
        _state.ComplexVisualState = null;
        SendMessage(message);
    }

    [Obsolete]
    internal void SystemReplic(string text) => UserMessage(MessageTypes.System, text);

    internal void ShowmanReplic(string text) => UserMessage(MessageTypes.Replic, text, GameRole.Showman);

    internal void ShowmanReplicNew(MessageCode messageCode) =>
        SendMessageWithArgs(
            Messages.ShowmanReplic,
            Random.Shared.Next(1, 20) /* used to select the same one of possible replics by all clients */,
            messageCode.ToString());

    internal void ShowmanReplicNew(MessageCode messageCode, string arg) =>
        SendMessageWithArgs(
            Messages.ShowmanReplic,
            Random.Shared.Next(1, 20) /* used to select the same one of possible replics by all clients */,
            messageCode.ToString(),
            arg);

    [Obsolete]
    internal void PlayerReplic(int playerIndex, string text) => UserMessage(MessageTypes.Replic, text, GameRole.Player, playerIndex);

    /// <summary>
    /// Пользовательское сообщение
    /// </summary>
    /// <param name="messageType">Тип сообщения</param>
    /// <param name="text">Текст сообщения</param>
    /// <param name="personRole">Роль источника сообщения (для реплик)</param>
    /// <param name="personIndex">Индекс источника сообщения (для реплик игроков)</param>
    internal void UserMessage(MessageTypes messageType, string text, GameRole? personRole = null, int? personIndex = null)
    {
        var person = messageType == MessageTypes.System
            ? ReplicCodes.System.ToString()
            : (personRole == GameRole.Player && personIndex.HasValue
                ? ReplicCodes.Player + personIndex.Value.ToString()
                : ReplicCodes.Showman.ToString());

        SendMessageWithArgs(Messages.Replic, person, text);
    }

    /// <summary>
    /// Выдача информации о счёте
    /// </summary>
    /// <param name="person">Кому выдаётся</param>
    public void InformSums(string person = NetworkConstants.Everybody)
    {
        var message = new StringBuilder(Messages.Sums);

        for (var i = 0; i < _state.Players.Count; i++)
        {
            message.Append(Message.ArgsSeparatorChar).Append(_state.Players[i].Sum);
        }

        SendMessage(message.ToString(), person);
    }

    /// <summary>
    /// Sends all rounds names to person.
    /// </summary>
    /// <param name="person">Person name.</param>
    public void InformRoundsNames(string person = NetworkConstants.Everybody)
    {
        var message = new StringBuilder(Messages.RoundsNames);

        for (var i = 0; i < _state.Rounds.Length; i++)
        {
            message.Append(Message.ArgsSeparatorChar).Append(_state.Rounds[i].Name);
        }

        SendMessage(message.ToString(), person);
    }

    public void SendThemeInfo(int themeIndex = -1, bool animate = false, int? overridenQuestionCount = null)
    {
        var theme = themeIndex > -1 ? _state.Themes?[themeIndex] : _state.Theme;

        if (theme == null)
        {
            return; // No theme available
        }

        var themeInfo = theme.Info;

        var messageBuilder = new MessageBuilder(
            themeIndex > -1 ? Messages.Theme2 : Messages.Theme,
            theme.Name,
            overridenQuestionCount ?? theme.Questions.Count,
            animate ? '+' : '-',
            themeInfo.Comments.Text.EscapeNewLines(),
            themeInfo.Authors.Count)
            .AddRange(themeInfo.Authors)
            .Add(themeInfo.Sources.Count)
            .AddRange(themeInfo.Sources);

        SendVisualMessage(messageBuilder);
    }

    internal void InformTheme(string person)
    {
        var theme = _state.Theme;

        if (theme == null)
        {
            return; // No theme available
        }

        var message = new MessageBuilder(
            Messages.ThemeInfo,
            theme.Name,
            theme.Questions.Count,
            theme.Info.Comments.Text.EscapeNewLines())
            .ToString();

        SendMessage(message, person);
    }

    /// <summary>
    /// Informs receiver about round table state.
    /// </summary>
    public void InformTable(string receiver = NetworkConstants.Everybody)
    {
        var message = new StringBuilder(Messages.Table);

        for (var i = 0; i < _state.TInfo.RoundInfo.Count; i++)
        {
            for (var j = 0; j < _state.TInfo.RoundInfo[i].Questions.Count; j++)
            {
                message.Append(Message.ArgsSeparatorChar);
                message.Append(_state.TInfo.RoundInfo[i].Questions[j].Price);
            }

            message.Append(Message.ArgsSeparatorChar);
        }

        SendMessage(message.ToString(), receiver);
    }

    /// <summary>
    /// Informs about game stage change.
    /// </summary>
    public void InformStage(string person = NetworkConstants.Everybody)
    {
        var messageBuilder = new MessageBuilder(Messages.Stage, _state.Stage);
        SendMessage(messageBuilder.ToString(), person);
    }

    internal void InformRound(
        string roundName,
        int roundIndex,
        QuestionSelectionStrategyType roundStrategy) =>
        SendVisualMessageWithArgs(Messages.Stage, _state.Stage, roundName, roundIndex, roundStrategy);

    public void InformStageInfo(string person, int stageIndex) =>
        SendMessageToWithArgs(person, Messages.StageInfo, _state.Stage.ToString(), _state.Round?.Name ?? "", stageIndex);

    internal void InformRoundThemesNames(string person = NetworkConstants.Everybody, ThemesPlayMode playMode = ThemesPlayMode.None)
    {
        var msg = new StringBuilder(Messages.RoundThemes)
            .Append(Message.ArgsSeparatorChar)
            .Append(playMode != ThemesPlayMode.None ? '+' : '-')
            .Append(Message.ArgsSeparatorChar)
            .Append(string.Join(Message.ArgsSeparator, _state.TInfo.RoundInfo.Select(info => info.Name)));

        SendMessage(msg.ToString(), person);

        var messageBuilder = new MessageBuilder(Messages.RoundThemes2, playMode).AddRange(_state.TInfo.RoundInfo.Select(info => info.Name));
        SendMessage(messageBuilder.ToString(), person);
    }

    internal void InformRoundThemesComments(string person = NetworkConstants.Everybody)
    {
        if (_state.ThemeComments.All(comment => comment.Length == 0))
        {
            return;
        }

        var messageBuilder = new MessageBuilder(Messages.RoundThemesComments).AddRange(_state.ThemeComments);
        SendMessage(messageBuilder.ToString(), person);
    }

    /// <summary>
    /// Sends links to all round media content to the person. This allows the person to preload content in advance.
    /// </summary>
    /// <param name="person">Person name (everybody by default).</param>
    internal void InformRoundContent(string person = NetworkConstants.Everybody)
    {
        if (!_state.Settings.AppSettings.PreloadRoundContent)
        {
            return;
        }

        IEnumerable<string> persons;

        if (person != NetworkConstants.Everybody)
        {
            if (Client.CurrentNode.Contains(person))
            {
                return; // local person does not need to preload anything
            }

            persons = new string[] { person };
        }
        else
        {
            // local persons do not need to preload anything
            var personsList = _state.AllPersons.Keys.Where(name => !Client.CurrentNode.Contains(name)).ToList();

            if (personsList.Count == 0)
            {
                return;
            }

            persons = personsList;
        }

        var contentUris = new HashSet<string>();

        foreach (var theme in _state.Round.Themes)
        {
            foreach (var question in theme.Questions)
            {
                foreach (var contentItem in question.GetContent())
                {
                    var contentType = contentItem.Type;

                    switch (contentType)
                    {
                        case ContentTypes.Image:
                        case ContentTypes.Audio:
                        case ContentTypes.Video:
                        case ContentTypes.Html:
                            {
                                if (!contentItem.IsRef) // External link
                                {
                                    if (contentType == ContentTypes.Html)
                                    {
                                        continue;
                                    }

                                    var link = contentItem.Value;

                                    if (Uri.TryCreate(link, UriKind.Absolute, out _))
                                    {
                                        contentUris.Add(link);
                                    }
                                }
                                else
                                {
                                    var mediaCategory = CollectionNames.TryGetCollectionName(contentType) ?? contentType;
                                    var media = _state.PackageDoc.TryGetMedia(contentItem);

                                    if (!media.HasValue || media.Value.Uri == null)
                                    {
                                        continue;
                                    }

                                    var mediaUri = media.Value.Uri;

                                    if (mediaUri.Scheme != "file")
                                    {
                                        contentUris.Add(mediaUri.ToString());
                                    }
                                    else
                                    {
                                        var fileName = Path.GetFileName(mediaUri.AbsolutePath);

                                        var resourceUri = _fileShare.CreateResourceUri(
                                            ResourceKind.Package,
                                            new Uri($"{mediaCategory}/{fileName}", UriKind.Relative));

                                        contentUris.Add(resourceUri);
                                    }
                                }

                                break;
                            }
                    }
                }
            }
        }

        if (contentUris.Count > 0)
        {
            var msg = new StringBuilder(Messages.RoundContent);

            foreach (var uri in contentUris)
            {
                msg.Append(Message.ArgsSeparatorChar).Append(uri);
            }

            foreach (var name in persons)
            {
                SendMessage(msg.ToString(), name);
            }
        }
    }

    internal void InformLayout(string person = NetworkConstants.Everybody)
    {
        var screenContentSequence = _state.QuestionPlay.ScreenContentSequence;
        var answerOptions = _state.QuestionPlay.AnswerOptions;

        if (answerOptions == null || screenContentSequence == null)
        {
            return;
        }

        var messageBuilder = new MessageBuilder(Messages.Layout)
            .Add(MessageParams.Layout_AnswerOptions)
            .Add(string.Join("|", screenContentSequence.Select(group => string.Join("+", group.Select(serializeContentItem)))))
            .AddRange(answerOptions.Select(o => o.Content.Type));

        var message = messageBuilder.ToString();

        SendMessage(message, person);

        static string serializeContentItem(ContentItem ci) => $"{ci.Type}{(ci.Type == ContentTypes.Text ? "." + ci.Value.Length : "")}";
    }

    internal void SendContentShape(string person = NetworkConstants.Everybody)
    {
        // ContentShapeCharacter symbol is used as an arbitrary symbol with medium width to define the question text shape
        // Real question text is sent later and it sequentially replaces test shape
        // Text shape is required to display partial question on the screen correctly
        // (font size and number of lines must be calculated in the beginning to prevent UI flickers on question text growth)
        var shape = Regex.Replace(_state.Text, "[^\r\n\t\f ]", ContentShapeCharacter);
        SendMessageToWithArgs(person, Messages.TextShape, shape);
        SendMessageToWithArgs(person, Messages.ContentShape, ContentPlacements.Screen, 0, ContentTypes.Text, shape.EscapeNewLines());
    }

    internal void InformQuestionCounter(int questionIndex, string person = NetworkConstants.Everybody) =>
        SendMessageToWithArgs(person, Messages.QuestionCounter, questionIndex);

    internal void InformAnswerDeviation(int deviation, string person = NetworkConstants.Everybody) =>
        SendMessageToWithArgs(person, Messages.AnswerDeviation, deviation);

    internal void AskAnswer(string person, string answerType) => SendMessageToWithArgs(person, Messages.Answer, answerType);

    internal void AskReview()
    {
        var packageSource = _state.GameResultInfo.PackageSource?.ToString() ?? "";

        if (!ReviewablePackageSources.Any(allowed => packageSource.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)))
        {
            packageSource = "";
        }

        foreach (var player in _state.Players)
        {
            SendMessageToWithArgs(player.Name, Messages.AskReview, packageSource);
        }
    }
}
