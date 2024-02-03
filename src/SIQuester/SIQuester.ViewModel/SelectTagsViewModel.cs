using SIQuester.ViewModel.Properties;

namespace SIQuester.ViewModel;

public sealed class SelectTagsViewModel
{
    public Tag[] Tags { get; }

    public Tag CommonTag { get; } = new(Resources.CommonTag);

    public SelectTagsViewModel(string[] tags) => Tags = tags.Select(t => new Tag(t)).ToArray();

    public string[] SelectedTags => Tags.Concat(new[] { CommonTag }).Where(t => t.IsSelected).Select(t => t.Name).ToArray();

    public sealed record Tag(string Name, bool IsSelected = false);
}
