using SIQuester.ViewModel.Properties;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Allows to set package tags.
/// </summary>
public sealed class SelectTagsViewModel : INotifyPropertyChanged
{
    public static TagGroup[] TagsGroups { get; private set; }

    static SelectTagsViewModel()
    {
        var tagGroups = new List<TagGroup>();
        
        var lines = Resources.DefaultTags.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var comparer = CultureInfo.CurrentUICulture.CompareInfo.GetStringComparer(CompareOptions.None);

        var commonTags = new List<string>();
        tagGroups.Add(new TagGroup(Resources.CommonTag, new[] {new Tag(Resources.CommonTag) }));

        foreach (var line in lines)
        {
            if (line.Contains(':'))
            {
                var lineParts = line.Split(':');
                var groupName = lineParts[0].Trim();
                var tags = lineParts[1].Split(',').Union(new[] { groupName }).OrderBy(t => t, comparer).Select(t => new Tag(t)).ToArray();
                tagGroups.Add(new TagGroup(groupName, tags));
            }
            else
            {
                commonTags.Add(line);
            }
        }

        tagGroups.Insert(1, new TagGroup(Resources.CommonTags, commonTags.OrderBy(t => t, comparer).Select(t => new Tag(t)).ToArray()));

        TagsGroups = tagGroups.ToArray();
    }

    private string _newItem = "";

    public string NewItem
    {
        get => _newItem;
        set
        {
            if (_newItem != value)
            {
                _newItem = value;
                OnPropertyChanged();
                AddItem.CanBeExecuted = _newItem.Length > 0;
            }
        }
    }

    public ObservableCollection<string> Items { get; } = new();

    public SimpleCommand AddItem { get; private set; }

    public SimpleCommand AddKnownItem { get; private set; }

    public SimpleCommand RemoveItem { get; private set; }

    public SelectTagsViewModel(ItemsViewModel<string> items)
    {
        Items = new ObservableCollection<string>(items);

        AddItem = new SimpleCommand(AddItem_Executed);
        AddKnownItem = new SimpleCommand(AddKnownItem_Executed);
        RemoveItem = new SimpleCommand(RemoveItem_Executed);
    }

    private void AddItem_Executed(object? arg)
    {
        var items = NewItem.Split(',');
        var added = false;

        foreach (var item in items)
        {
            var value = item.Trim();

            if (value.Length > 0 && !Items.Contains(value))
            {
                Items.Add(value);
                added = true;
            }
        }
        
        NewItem = "";

        if (added)
        {
            OnPropertyChanged(nameof(Items));
        }
    }

    private void AddKnownItem_Executed(object? arg)
    {
        var value = arg?.ToString();

        if (value == null)
        {
            return;
        }

        if (Items.Contains(value))
        {
            Items.Remove(value);
        }
        else
        {
            Items.Add(value);
        }

        OnPropertyChanged(nameof(Items));
    }

    private void RemoveItem_Executed(object? arg)
    {
        var value = arg?.ToString();

        if (value == null)
        {
            return;
        }

        Items.Remove(value);
        OnPropertyChanged(nameof(Items));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public sealed record Tag(string Name);

    public sealed record TagGroup(string Name, Tag[] Tags);
}
