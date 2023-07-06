using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SIData;

/// <inheritdoc cref="IGameSettingsCore{T}" />
[DataContract]
public class GameSettingsCore<T> : IGameSettingsCore<T>
    where T: AppSettingsCore, new()
{
    /// <summary>
    /// Game host name.
    /// </summary>
    [DataMember]
    public string HumanPlayerName { get; set; }

    /// <summary>
    /// Случайные спецвопросы
    /// </summary>
    [XmlAttribute]
    [DefaultValue(false)]
    [DataMember]
    public bool RandomSpecials { get; set; }

    /// <summary>
    /// Имя сетевой игры
    /// </summary>
    [XmlAttribute]
    [DefaultValue("")]
    [DataMember]
    public string NetworkGameName { get; set; } = "";

    /// <summary>
    /// Пароль сетевой игры
    /// </summary>
    [XmlAttribute]
    [DefaultValue("")]
    [DataMember]
    public string NetworkGamePassword { get; set; } = "";

    /// <summary>
    /// Network voice chat link.
    /// </summary>
    [DataMember]
    public string NetworkVoiceChat { get; set; } = "";

    [XmlAttribute]
    [DefaultValue(false)]
    [DataMember]
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Ведущий игры
    /// </summary>
    [XmlIgnore]
    [DataMember]
    public Account Showman { get; set; }

    /// <summary>
    /// Игроки
    /// </summary>
    [XmlIgnore]
    [DataMember]
    public Account[] Players { get; set; }

    /// <summary>
    /// Зрители
    /// </summary>
    [XmlIgnore]
    [DataMember]
    public Account[] Viewers { get; set; } = Array.Empty<Account>();

    /// <summary>
    /// User-defines game preferences and rules.
    /// </summary>
    [DataMember]
    public T AppSettings { get; set; } = new T();

    [DefaultValue(false)]
    [DataMember]
    public bool IsAutomatic { get; set; }
}
