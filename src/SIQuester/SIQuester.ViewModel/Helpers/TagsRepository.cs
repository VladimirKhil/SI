using SIQuester.ViewModel.Properties;
using System.Globalization;

namespace SIQuester.ViewModel.Helpers;

public sealed class TagsRepository
{
    public static TagsRepository Instance { get; } = new TagsRepository();

    public TagGroup[] TagsGroups { get; }

    public Dictionary<string, string> Translation { get; } = new();

    public TagsRepository()
    {
        var tagGroups = new List<TagGroup>();

        var lines = Resources.DefaultTags.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var comparer = CultureInfo.CurrentUICulture.CompareInfo.GetStringComparer(CompareOptions.None);

        var commonTags = new List<string>();
        tagGroups.Add(new TagGroup(Resources.CommonTag, new[] { new Tag(Resources.CommonTag) }));

        Translation[Resources.CommonTag] = "General knowledge";

        foreach (var line in lines)
        {
            if (line.Contains(':'))
            { 
                var lineParts = line.Split(':');
                var groupName = lineParts[0].Trim();
                var tagsWithLocalization = lineParts[1].Split(',');
                var tagNames = new List<string>();

                foreach (var item in tagsWithLocalization)
                {
                    tagNames.Add(ProcessTag(item));
                }

                groupName = ProcessTag(groupName);

                var tags = tagNames.Union(new[] { groupName }).OrderBy(t => t, comparer).Select(t => new Tag(t)).ToArray();
                tagGroups.Add(new TagGroup(groupName, tags));
            }
            else
            {
                commonTags.Add(ProcessTag(line));
            }
        }

        tagGroups.Insert(1, new TagGroup(Resources.CommonTags, commonTags.OrderBy(t => t, comparer).Select(t => new Tag(t)).ToArray()));

        TagsGroups = tagGroups.ToArray();
    }

    private string ProcessTag(string item)
    {
        var translationMarkIndex = item.IndexOf('|');

        if (translationMarkIndex > 0 && translationMarkIndex < item.Length - 1)
        {
            var tag = item[..translationMarkIndex].Trim();
            var translation = item[(translationMarkIndex + 1)..].Trim();

            if (!string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(translation))
            {
                Translation[tag] = translation;
            }

            return tag;
        }
        else
        {
            return item.Trim();
        }
    }

    public sealed record Tag(string Name);

    public sealed record TagGroup(string Name, Tag[] Tags);
}
