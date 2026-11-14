using SIQuester.ViewModel.Helpers;
using static SIQuester.ViewModel.Helpers.TagsRepository;

namespace SIQuester.ViewModel.Tests;

/// <summary>
/// Tests for filtering the tags catalog by the typed text.
/// </summary>
[TestFixture]
internal sealed class TagsFilterTests
{
    private static readonly TagGroup Movies = new("Кино", [new Tag("Комедии"), new Tag("Мультфильмы")]);
    private static readonly TagGroup Science = new("Наука", [new Tag("Физика"), new Tag("Химия")]);

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("  физ ", "физ")]
    [TestCase("История, физ", "физ")]
    [TestCase("История,", "")]
    public void GetFilter_ReturnsTagBeingTyped(string? newItem, string expected) =>
        Assert.That(SelectTagsViewModel.GetFilter(newItem), Is.EqualTo(expected));

    [Test]
    public void EmptyFilter_ShowsEverything()
    {
        Assert.That(SelectTagsViewModel.IsGroupVisible(Science, ""), Is.True);
        Assert.That(SelectTagsViewModel.IsTagVisible(Science, Science.Tags[0], ""), Is.True);
    }

    [Test]
    public void TagFilter_ShowsMatchingTagsAndTheirGroup_IgnoringCase()
    {
        Assert.That(SelectTagsViewModel.IsGroupVisible(Science, "ФИЗ"), Is.True);
        Assert.That(SelectTagsViewModel.IsTagVisible(Science, Science.Tags[0], "ФИЗ"), Is.True);
        Assert.That(SelectTagsViewModel.IsTagVisible(Science, Science.Tags[1], "ФИЗ"), Is.False);

        Assert.That(SelectTagsViewModel.IsGroupVisible(Movies, "ФИЗ"), Is.False);
    }

    [Test]
    public void GroupFilter_ShowsAllTagsOfTheGroup()
    {
        Assert.That(SelectTagsViewModel.IsGroupVisible(Movies, "кино"), Is.True);
        Assert.That(Movies.Tags.All(tag => SelectTagsViewModel.IsTagVisible(Movies, tag, "кино")), Is.True);

        Assert.That(SelectTagsViewModel.IsGroupVisible(Science, "кино"), Is.False);
    }

    [Test]
    public void RealCatalog_TypingAKnownTag_FindsIt()
    {
        var group = TagsRepository.Instance.TagsGroups.First(g => g.Tags.Length > 0);
        var tag = group.Tags[0];

        Assert.That(SelectTagsViewModel.IsGroupVisible(group, tag.Name), Is.True);
        Assert.That(SelectTagsViewModel.IsTagVisible(group, tag, tag.Name), Is.True);
    }
}
