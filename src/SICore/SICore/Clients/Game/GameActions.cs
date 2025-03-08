using Notions;
using SICore.Clients;
using SICore.Contracts;
using SICore.Extensions;
using SICore.Models;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Utils;
using SIData;
using SIEngine.Rules;
using SIPackages;
using SIPackages.Core;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Defines game actions.
/// </summary>
public sealed class GameActions
{
    private readonly GameData _gameData;

    private readonly ILocalizer LO;

    public Client Client { get; }

    private readonly IFileShare _fileShare;

    public GameActions(Client client, GameData gameData, ILocalizer localizer, IFileShare fileShare)
    {
        Client = client;
        _gameData = gameData;
        LO = localizer;
        _fileShare = fileShare;
    }

    public void SendMessage(string text, string receiver = NetworkConstants.Everybody) => Client.SendMessage(text, true, receiver);

    public void SendMessageWithArgs(params object[] args) => SendMessage(string.Join(Message.ArgsSeparator, args));

    public void SendMessageToWithArgs(string receiver, params object[] args) =>
        SendMessage(string.Join(Message.ArgsSeparator, args), receiver);

    internal void SystemReplic(string text) => UserMessage(MessageTypes.System, text);

    internal void SpecialReplic(string text) => UserMessage(MessageTypes.Special, text);

    internal void ShowmanReplic(string text) => UserMessage(MessageTypes.Replic, text, GameRole.Showman);

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
            : messageType == MessageTypes.Special
                ? ReplicCodes.Special.ToString()
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

        for (var i = 0; i < _gameData.Players.Count; i++)
        {
            message.Append(Message.ArgsSeparatorChar).Append(_gameData.Players[i].Sum);
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

        for (var i = 0; i < _gameData.Rounds.Length; i++)
        {
            message.Append(Message.ArgsSeparatorChar).Append(LO.GetRoundName(_gameData.Rounds[i].Name));
        }

        SendMessage(message.ToString(), person);
    }

    /// <summary>
    /// Объявить суммы
    /// </summary>
    public void AnnounceSums()
    {
        var s = new StringBuilder(LO[nameof(R.Score)]).Append(": ");
        var playerCount = _gameData.Players.Count;

        for (var i = 0; i < playerCount; i++)
        {
            s.Append(Notion.FormatNumber(_gameData.Players[i].Sum));

            if (i < playerCount - 1)
            {
                s.Append("; ");
            }
        }

        SystemReplic(s.ToString());
    }

    public void SendThemeInfo(int themeIndex = -1, bool animate = false, int? overridenQuestionCount = null)
    {
        var theme = themeIndex > -1 ? _gameData.Themes[themeIndex] : _gameData.Theme;
        var themeInfo = theme.Info;

        var message = new MessageBuilder(
            themeIndex > -1 ? Messages.Theme2 : Messages.Theme,
            theme.Name,
            overridenQuestionCount ?? theme.Questions.Count,
            animate ? '+' : '-',
            themeInfo.Comments.Text.EscapeNewLines(),
            themeInfo.Authors.Count)
            .AddRange(themeInfo.Authors)
            .Add(themeInfo.Sources.Count)
            .AddRange(themeInfo.Sources)
            .ToString();

        SendMessage(message);
    }

    internal void InformTheme(string person)
    {
        var theme = _gameData.Theme;

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

        for (var i = 0; i < _gameData.TInfo.RoundInfo.Count; i++)
        {
            for (var j = 0; j < _gameData.TInfo.RoundInfo[i].Questions.Count; j++)
            {
                message.Append(Message.ArgsSeparatorChar);
                message.Append(_gameData.TInfo.RoundInfo[i].Questions[j].Price);
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
        var messageBuilder = new MessageBuilder(Messages.Stage, _gameData.Stage);
        SendMessage(messageBuilder.ToString(), person);
    }

    internal void InformRound(
        string roundName,
        int roundIndex,
        QuestionSelectionStrategyType roundStrategy,
        string person = NetworkConstants.Everybody)
    {
        var messageBuilder = new MessageBuilder(Messages.Stage, _gameData.LegacyStage, roundName, roundIndex, roundStrategy);
        SendMessage(messageBuilder.ToString(), person);
    }

    public void InformStageInfo(string person, int stageIndex) =>
        SendMessageToWithArgs(person, Messages.StageInfo, _gameData.Stage.ToString(), _gameData.Round?.Name ?? "", stageIndex);

    internal void InformRoundThemesNames(string person = NetworkConstants.Everybody, ThemesPlayMode playMode = ThemesPlayMode.None)
    {
        var msg = new StringBuilder(Messages.RoundThemes)
            .Append(Message.ArgsSeparatorChar)
            .Append(playMode != ThemesPlayMode.None ? '+' : '-')
            .Append(Message.ArgsSeparatorChar)
            .Append(string.Join(Message.ArgsSeparator, _gameData.TInfo.RoundInfo.Select(info => info.Name)));

        SendMessage(msg.ToString(), person);

        var messageBuilder = new MessageBuilder(Messages.RoundThemes2, playMode).AddRange(_gameData.TInfo.RoundInfo.Select(info => info.Name));
        SendMessage(messageBuilder.ToString(), person);
    }

    internal void InformRoundThemesComments(string person = NetworkConstants.Everybody)
    {
        if (_gameData.ThemeComments.All(comment => comment.Length == 0))
        {
            return;
        }

        var messageBuilder = new MessageBuilder(Messages.RoundThemesComments).AddRange(_gameData.ThemeComments);
        SendMessage(messageBuilder.ToString(), person);
    }

    /// <summary>
    /// Sends links to all round media content to the person. This allows the person to preload content in advance.
    /// </summary>
    /// <param name="person">Person name (everybody by default).</param>
    internal void InformRoundContent(string person = NetworkConstants.Everybody)
    {
        if (!_gameData.Settings.AppSettings.PreloadRoundContent)
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
            var personsList = _gameData.AllPersons.Keys.Where(name => !Client.CurrentNode.Contains(name)).ToList();

            if (personsList.Count == 0)
            {
                return;
            }

            persons = personsList;
        }

        var contentUris = new HashSet<string>();

        foreach (var theme in _gameData.Round.Themes)
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
                                    var media = _gameData.PackageDoc.TryGetMedia(contentItem);

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
}
