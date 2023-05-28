using System.Xml.Serialization;

namespace SIGame.ViewModel;

/// <summary>
/// Лучший игрок
/// </summary>
public sealed class BestPlayer
{
    /// <summary>
    /// Количество лучших игроков
    /// </summary>
    public const int Total = 10;

    /// <summary>
    /// Имя лучшего игрока
    /// </summary>
    [XmlAttribute("name")]
    public string Name { get; set; }

    /// <summary>
    /// Выигрыш лучшего игрока
    /// </summary>
    [XmlAttribute("sum")]
    public int Result { get; set; }

    /// <summary>
    /// Пустая запись о лучшем игроке
    /// </summary>
    public BestPlayer()
    {
        Name = "";
        Result = 0;
    }

    public override string ToString() => $"{Name}: {Result}";
}
