using Notions;
using SICore.BusinessLogic;
using SICore.Clients;
using SICore.Contracts;
using SICore.Network;
using SICore.Network.Clients;
using SIData;
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
                : (personRole == GameRole.Player
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
    /// Sends all rounds names to person. Only rounds with at least one question are taken into account.
    /// </summary>
    /// <param name="person">Person name.</param>
    public void InformRoundsNames(string person = NetworkConstants.Everybody)
    {
        var message = new StringBuilder(Messages.RoundsNames);

        for (var i = 0; i < _gameData.Rounds.Length; i++)
        {
            message.Append(Message.ArgsSeparatorChar).Append(_gameData.Rounds[i].Name);
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
    /// Выдача информации о состоянии игры
    /// </summary>
    public void InformStage(string person = NetworkConstants.Everybody, string name = null, int index = -1)
    {
        if (index > -1)
        {
            SendMessage(string.Join(Message.ArgsSeparator, Messages.Stage, _gameData.Stage.ToString(), name ?? "", index), person);
        }
        else
        {
            SendMessage(string.Join(Message.ArgsSeparator, Messages.Stage, _gameData.Stage.ToString(), name ?? ""), person);
        }
    }

    internal void InformRoundThemes(string person = NetworkConstants.Everybody, bool play = true)
    {
        var msg = new StringBuilder(Messages.RoundThemes)
            .Append(Message.ArgsSeparatorChar)
            .Append(play ? '+' : '-')
            .Append(Message.ArgsSeparatorChar)
            .Append(string.Join(Message.ArgsSeparator, _gameData.TInfo.RoundInfo.Select(info => info.Name)));

        SendMessage(msg.ToString(), person);
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
            if (Client.CurrentServer.Contains(person))
            {
                return; // local person does not need to preload anything
            }

            persons = new string[] { person };
        }
        else
        {
            // local persons do not need to preload anything
            var personsList = _gameData.AllPersons.Keys.Where(name => !Client.CurrentServer.Contains(name)).ToList();

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
                        case AtomTypes.Image:
                        case AtomTypes.Audio:
                        case AtomTypes.AudioNew:
                        case AtomTypes.Video:
                        case AtomTypes.Html:
                            {
                                if (!contentItem.IsRef) // External link
                                {
                                    if (contentType == AtomTypes.Html)
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

                                        var externalUri = resourceUri.ToString().Replace("http://localhost", "http://" + Constants.GameHost);
                                        contentUris.Add(externalUri);
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
