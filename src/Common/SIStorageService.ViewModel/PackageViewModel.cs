using SIStorage.Service.Contract.Models;

namespace SIStorageService.ViewModel;

/// <summary>
/// Defines a SIStorage package view model.
/// </summary>
public sealed class PackageViewModel
{
    /// <summary>
    /// Original package.
    /// </summary>
    public Package Model { get; }

    /// <summary>
    /// Package tags.
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Package publisher.
    /// </summary>
    public string Publisher { get; }

    /// <summary>
    /// Package restrictions.
    /// </summary>
    public string[] Restrictions { get; }

    /// <summary>
    /// Package authors.
    /// </summary>
    public string[] Authors { get; }

    public PackageViewModel(Package package, Tag[] tags, Publisher[] publishers, Restriction[] restrictions, Author[] authors)
    {
        Model = package;
        Tags = package.TagIds.Select(tagId => tags.FirstOrDefault(t => t.Id == tagId)?.Name ?? "").ToArray();
        Publisher = publishers.FirstOrDefault(p => p.Id == package.PublisherId)?.Name ?? "";

        Restrictions = package.RestrictionIds?.Select(restrictionId => restrictions.FirstOrDefault(r => r.Id == restrictionId)?.Value ?? "").ToArray()
            ?? Array.Empty<string>();

        Authors = package.AuthorIds.Select(authorId => authors.FirstOrDefault(a => a.Id == authorId)?.Name ?? "").ToArray();
    }
}
