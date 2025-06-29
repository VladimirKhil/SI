using Steamworks;

namespace SIQuester.Model;

/// <summary>
/// Represents a Steam Workshop item.
/// </summary>
public sealed class WorkshopItem
{
    /// <summary>
    /// Steam published file ID.
    /// </summary>
    public PublishedFileId_t PublishedFileId { get; set; }

    /// <summary>
    /// Item title/name.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Item description.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Item tags.
    /// </summary>
    public string Tags { get; set; } = "";

    /// <summary>
    /// Workshop item URL.
    /// </summary>
    public string Url => $"https://steamcommunity.com/sharedfiles/filedetails/?id={PublishedFileId}";
}