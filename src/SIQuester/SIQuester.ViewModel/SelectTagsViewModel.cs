using SIQuester.ViewModel.Properties;
using System.Globalization;

namespace SIQuester.ViewModel;

public sealed class SelectTagsViewModel
{
    private static readonly string[] _defaultTags = Resources.DefaultTags.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

    public Tag[] Tags { get; }

    public Tag CommonTag { get; } = new(Resources.CommonTag);

    public SelectTagsViewModel(string[] tags) => Tags = tags
        .Union(_defaultTags)
        .OrderBy(t => t, CultureInfo.CurrentUICulture.CompareInfo.GetStringComparer(CompareOptions.None))
        .Select(t => new Tag(t))
        .ToArray();

    public string[] SelectedTags => Tags.Concat(new[] { CommonTag }).Where(t => t.IsSelected).Select(t => t.Name).ToArray();

    public sealed record Tag(string Name, bool IsSelected = false);
}
