using SIQuester.ViewModel.Workspaces.Dialogs;
using Steamworks;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for finding an already published Workshop item before uploading a package again.
/// </summary>
[TestFixture]
internal sealed class SteamDuplicateTests
{
    private static WorkshopItemViewModel Item(ulong id, string title, string packageId = "") =>
        new(new PublishedFileId_t(id)) { Title = title, PackageId = packageId };

    [Test]
    public void SamePackageId_IsFound_EvenIfRenamed()
    {
        var items = new[] { Item(1, "Old name", "pkg-1"), Item(2, "Other", "pkg-2") };

        var found = ExportToSteamViewModel.FindPublishedItem(items, "New name", "pkg-1");

        Assert.That(found?.PublishedFileId.m_PublishedFileId, Is.EqualTo(1UL));
    }

    [Test]
    public void SameTitle_DifferentPackageId_IsNotADuplicate()
    {
        var items = new[] { Item(1, "Pack", "pkg-1") };

        Assert.That(ExportToSteamViewModel.FindPublishedItem(items, "Pack", "pkg-2"), Is.Null);
    }

    [Test]
    public void OldItemWithoutPackageId_IsMatchedByTitle()
    {
        var items = new[] { Item(1, " My Pack ") };

        var found = ExportToSteamViewModel.FindPublishedItem(items, "my pack", "pkg-1");

        Assert.That(found?.PublishedFileId.m_PublishedFileId, Is.EqualTo(1UL));
    }

    [Test]
    public void NewPackage_IsNotFound()
    {
        var items = new[] { Item(1, "Pack A", "pkg-1"), Item(2, "Pack B") };

        Assert.That(ExportToSteamViewModel.FindPublishedItem(items, "Pack C", "pkg-3"), Is.Null);
    }
}
