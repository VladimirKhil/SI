using SIQuester.ViewModel.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Allows to set package tags.
/// </summary>
public sealed class SelectTagsViewModel : INotifyPropertyChanged
{
    public static TagsRepository.TagGroup[] TagsGroups { get; private set; }

    static SelectTagsViewModel() => TagsGroups = TagsRepository.Instance.TagsGroups;

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

    /// <summary>
    /// Gets the text that filters the tags catalog: the tag being typed (the part after the last comma).
    /// </summary>
    public static string GetFilter(string? newItem)
    {
        if (string.IsNullOrEmpty(newItem))
        {
            return "";
        }

        var lastSeparator = newItem.LastIndexOf(',');
        return newItem[(lastSeparator + 1)..].Trim();
    }

    /// <summary>
    /// Is the catalog tag shown for the filter: the tag or its whole group matches.
    /// </summary>
    public static bool IsTagVisible(TagsRepository.TagGroup group, TagsRepository.Tag tag, string filter) =>
        filter.Length == 0 || Matches(tag.Name, filter) || Matches(group.Name, filter);

    /// <summary>
    /// Is the catalog group shown for the filter: the group or any of its tags matches.
    /// </summary>
    public static bool IsGroupVisible(TagsRepository.TagGroup group, string filter) =>
        filter.Length == 0 || Matches(group.Name, filter) || group.Tags.Any(tag => Matches(tag.Name, filter));

    private static bool Matches(string name, string filter) => name.Contains(filter, StringComparison.CurrentCultureIgnoreCase);

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
}
