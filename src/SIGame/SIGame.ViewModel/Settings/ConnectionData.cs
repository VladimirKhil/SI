using System.ComponentModel;
using System.Xml.Serialization;
using SIGame.ViewModel;

namespace SIGame;

/// <summary>
/// Информация о подключении к другому серверу
/// </summary>
public sealed class ConnectionData : IHumanPlayerOwner
{
    [XmlAttribute]
    [DefaultValue("")]
    public string Address { get; set; } = "";

    [XmlIgnore]
    public string HumanPlayerName
    {
        get { return UserSettings.Default.GameSettings.HumanPlayerName; }
        set { UserSettings.Default.GameSettings.HumanPlayerName = value; }
    }

    [XmlIgnore]
    public AppSettings AppSettings
    {
        get { return UserSettings.Default.GameSettings.AppSettings; }
    }
}
