using SIData;
using SIGame.ViewModel.PackageSources;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SIGame.ViewModel;

/// <summary>
/// Настройки игры
/// </summary>
[Serializable]
public sealed class GameSettings : GameSettingsCore<AppSettings>, IHumanPlayerOwner
{
    public PackageSourceKey PackageKey { get; set; }

    /// <summary>
    /// Будет ли игра сетевой
    /// </summary>
    [XmlAttribute]
    [DefaultValue(false)]
    public bool NetworkGame { get; set; } = false;

    /// <summary>
    /// Порт сетевой игры
    /// </summary>
    [XmlAttribute]
    [DefaultValue(7000)]
    public int NetworkPort { get; set; } = 7000;

    /// <summary>
    /// Тип сетевой игры
    /// </summary>
    [XmlAttribute]
    [DefaultValue(NetworkGameType.GameServer)]
    public NetworkGameType NetworkGameType { get; set; } = NetworkGameType.GameServer;

    /// <summary>
    /// Роль хоста в игре
    /// </summary>
    [XmlAttribute]
    [DefaultValue(GameRole.Player)]
    public GameRole Role { get; set; } = GameRole.Player;

    /// <summary>
    /// Номер за столом в роли игрока
    /// </summary>
    public int PlayerNumber => 0;

    /// <summary>
    /// Тип ведущего
    /// </summary>
    [XmlAttribute]
    [DefaultValue(AccountTypes.Computer)]
    public AccountTypes ShowmanType { get; set; } = AccountTypes.Computer;

    /// <summary>
    /// Типы игроков
    /// </summary>
    public AccountTypes[] PlayersTypes { get; set; }

    [XmlAttribute]
    [DefaultValue(3)]
    public int PlayersCount { get; set; } = 3;

    public event Action? Updated;

    public static explicit operator GameSettingsCore<AppSettingsCore>(GameSettings settings) =>
        new()
        {
            IsAutomatic = settings.IsAutomatic,
            IsPrivate = settings.IsPrivate,
            AppSettings = settings.AppSettings.ToAppSettingsCore(),
            HumanPlayerName = settings.HumanPlayerName,
            NetworkGameName = settings.NetworkGameName,
            NetworkGamePassword = settings.NetworkGamePassword,
            NetworkVoiceChat = settings.NetworkVoiceChat,
            Players = Convert(settings.Players),
            RandomSpecials = settings.RandomSpecials,
            Showman = Convert(settings.Showman),
            Viewers = Convert(settings.Viewers),
        };

    private static Account Convert(Account account) =>
        new() { IsHuman = account.IsHuman, Name = account.Name, Picture = account.Picture, IsMale = account.IsMale };

    private static Account[] Convert(Account[] accounts) => accounts.Select(acc => Convert(acc)).ToArray();

    public void OnUpdated() => Updated?.Invoke();
}
