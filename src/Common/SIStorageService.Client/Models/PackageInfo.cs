namespace SIStorageService.Client.Models;

/// <summary>
/// Contains information about game package.
/// </summary>
public sealed class PackageInfo
{
    /// <summary>
    /// Package identifier.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    /// Универсальный идентификатор
    /// </summary>
    public string? Guid { get; set; }

    /// <summary>
    /// Описание пакета
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Ограничения пакета
    /// </summary>
    public string? Restriction { get; set; }

    /// <summary>
    /// Авторы пакета
    /// </summary>
    public string? Authors { get; set; }

    /// <summary>
    /// Тематики пакета
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Сложность пакета (от 1 до 10)
    /// </summary>
    public int Difficulty { get; set; }

    /// <summary>
    /// Издатель пакета
    /// </summary>
    public string? Publisher { get; set; }

    /// <summary>
    /// Логотип
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// Логотип
    /// </summary>
    public DateTime? PublishedDate { get; set; }
}
