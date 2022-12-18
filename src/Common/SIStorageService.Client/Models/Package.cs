namespace SIStorageService.Client.Models;

/// <summary>
/// Defines package info.
/// </summary>
public sealed class Package
{
    /// <summary>
    /// Package identifier.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    /// Имя пакета
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Описание пакета
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Ограничения пакета
    /// </summary>
    public string? Restriction { get; set; }

    /// <summary>
    /// Значение хэша пакета
    /// </summary>
    public byte[]? Hash { get; set; }

    /// <summary>
    /// Уникальный идентификатор пакета
    /// </summary>
    public string? PackageID { get; set; }

    /// <summary>
    /// Тематический ли
    /// </summary>
    public bool IsTopical { get; set; }

    /// <summary>
    /// Дата последнего изменения пакета
    /// </summary>
    public DateTime? DateChanged { get; set; }
}
