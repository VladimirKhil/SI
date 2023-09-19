using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.PlatformSpecific;
using SIQuester.ViewModel.Properties;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a content item list view model.
/// </summary>
public sealed class ContentItemsViewModel : ItemsViewModel<ContentItemViewModel>
{
    private List<ContentItem> Model { get; }

    public QuestionViewModel Owner { get; private set; }

    public override QDocument? OwnerDocument => Owner?.OwnerTheme?.OwnerRound?.OwnerPackage?.Document;

    public ICommand AddText { get; private set; }

    public ICommand AddVoice { get; private set; }

    public ICommand ChangePlacement { get; private set; }

    public SimpleCommand SetTime { get; private set; }

    /// <summary>
    /// Joins content item with next one to play them together.
    /// </summary>
    public SimpleCommand JoinWithNext { get; private set; }

    public SimpleCommand CollapseMedia { get; private set; }

    public SimpleCommand ExpandMedia { get; private set; }

    public SimpleCommand ExportMedia { get; private set; }

    public ICommand SelectAtomObject { get; private set; }

    public bool IsTopLevel { get; private set; }

    public ContentItemsViewModel(QuestionViewModel question, List<ContentItem> contentItems, bool isTopLevel)
    {
        Owner = question;
        Model = contentItems;
        IsTopLevel = isTopLevel;

        foreach (var item in contentItems)
        {
            Add(new ContentItemViewModel(item) { Owner = this });
        }

        CollectionChanged += ContentItemsViewModel_CollectionChanged;

        AddText = new SimpleCommand(AddText_Executed);
        AddVoice = new SimpleCommand(AddVoice_Executed);

        ChangePlacement = new SimpleCommand(ChangePlacement_Executed);

        SetTime = new SimpleCommand(SetTime_Executed);
        JoinWithNext = new SimpleCommand(JoinWithNext_Executed);

        CollapseMedia = new SimpleCommand(CollapseMedia_Executed);
        ExpandMedia = new SimpleCommand(ExpandMedia_Executed);
        ExportMedia = new SimpleCommand(ExportMedia_Executed);

        SelectAtomObject = new SimpleCommand(SelectAtomObject_Executed);
        IsTopLevel = isTopLevel;
    }

    internal void AddText_Executed(object? arg) => QDocument.ActivatedObject = Add(AtomTypes.Text, "", ContentPlacements.Screen);

    private void AddVoice_Executed(object? arg)
    {
        var index = CurrentPosition;

        if (index > -1 && index < Count && string.IsNullOrWhiteSpace(this[index].Model.Value))
        {
            RemoveAt(index);
        }

        QDocument.ActivatedObject = Add(AtomTypes.Text, "", ContentPlacements.Replic);
    }

    private void ChangePlacement_Executed(object? arg)
    {
        var index = CurrentPosition;

        if (index > -1 && index < Count)
        {
            var contentItem = this[index];

            if (contentItem.Type != AtomTypes.Text)
            {
                return;
            }

            if (contentItem.Model.Placement == ContentPlacements.Screen)
            {
                contentItem.Model.Placement = ContentPlacements.Replic;
            }
            else if (contentItem.Model.Placement == ContentPlacements.Replic)
            {
                contentItem.Model.Placement = ContentPlacements.Screen;
            }
        }
    }

    internal ContentItemViewModel Add(string contentType, string value, string placement)
    {
        var contentItem = new ContentItemViewModel(new ContentItem { Type = contentType, Value = value, Placement = placement });
        Add(contentItem);

        return contentItem;
    }

    protected override void OnCurrentItemChanged(ContentItemViewModel oldValue, ContentItemViewModel newValue)
    {
        base.OnCurrentItemChanged(oldValue, newValue);

        if (oldValue != null)
        {
            oldValue.PropertyChanged -= CurrentAtom_PropertyChanged;
            oldValue.Model.PropertyChanged -= Model_PropertyChanged;
        }

        if (newValue != null)
        {
            newValue.PropertyChanged += CurrentAtom_PropertyChanged;
            newValue.Model.PropertyChanged += Model_PropertyChanged;
        }

        UpdateContentItemCommands();
    }

    protected override bool CanRemove() => Count > 1;

    private void CurrentAtom_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ContentItemViewModel.IsExpanded))
        {
            UpdateContentItemCommands();
        }
    }

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ContentItem.Duration))
        {
            UpdateContentItemCommands();
        }
    }

    private void ContentItemsViewModel_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems?.Count; i++)
                {
                    this[i].Owner = this;
                    Model.Insert(i, this[i].Model);
                }

                break;

            case NotifyCollectionChangedAction.Replace:
                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems?.Count; i++)
                {
                    this[i].Owner = this;
                    Model[i] = this[i].Model;
                }

                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (var contentItem in e.OldItems.Cast<ContentItemViewModel>())
                    {
                        contentItem.Owner = null;
                        Model.RemoveAt(e.OldStartingIndex);
                        OwnerDocument?.ClearLinks(contentItem.Model);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                Model.Clear();

                foreach (ContentItemViewModel contentItem in this)
                {
                    contentItem.Owner = this;
                    Model.Add(contentItem.Model);
                }

                break;
        }

        UpdateCommands();
    }

    private void UpdateContentItemCommands()
    {
        var contentItem = CurrentItem;
        var atomType = contentItem.Model.Type;

        var isMedia = atomType == AtomTypes.Image
            || atomType == AtomTypes.Audio
            || atomType == AtomTypes.AudioNew
            || atomType == AtomTypes.Video
            || atomType == AtomTypes.Html;

        SetTime.CanBeExecuted = contentItem != null && contentItem.Model.Duration == TimeSpan.Zero;

        CollapseMedia.CanBeExecuted = contentItem != null && isMedia && contentItem.IsExpanded;
        ExpandMedia.CanBeExecuted = contentItem != null && isMedia && !contentItem.IsExpanded;
        ExportMedia.CanBeExecuted = contentItem != null && isMedia;
    }

    private void SetTime_Executed(object? arg)
    {
        QDocument.ActivatedObject = CurrentItem;
        CurrentItem.Model.Duration = TimeSpan.FromSeconds(5);
    }

    private void JoinWithNext_Executed(object? arg)
    {
        CurrentItem.Model.WaitForFinish = !CurrentItem.Model.WaitForFinish;
    }

    private void CollapseMedia_Executed(object? arg)
    {
        CurrentItem.IsExpanded = false;
        CollapseMedia.CanBeExecuted = false;
        ExpandMedia.CanBeExecuted = true;
    }

    private void ExpandMedia_Executed(object? arg)
    {
        CurrentItem.IsExpanded = true;
        CollapseMedia.CanBeExecuted = true;
        ExpandMedia.CanBeExecuted = false;
    }

    private void ExportMedia_Executed(object? arg)
    {
        try
        {
            var document = Owner.OwnerTheme.OwnerRound?.OwnerPackage?.Document;

            if (document == null)
            {
                throw new InvalidOperationException("document is undefined");
            }

            var media = document.Document.GetLink(CurrentItem.Model);

            if (media.GetStream != null && media.Uri != null)
            {
                var fileName = media.Uri;

                if (PlatformManager.Instance.ShowSaveUI(Resources.ExportMedia, "", null, ref fileName))
                {
                    using var stream = media.GetStream().Stream;
                    using var fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);

                    stream.CopyTo(fileStream);
                }
            }
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowExclamationMessage($"{Resources.ExportMediaError}: {exc}");
        }
    }

    private void SelectAtomObject_Executed(object? arg) => SelectAtomObjectCore(arg);

    public bool SelectAtomObjectCore(object? arg)
    {
        var data = (Tuple<object, object>)arg;
        var media = data.Item1;
        var mediaType = data.Item2.ToString() ?? "";

        if (media is MediaItemViewModel file)
        {
            SelectAtomObject_Do(mediaType, file);
            return false;
        }

        if (media is not string text)
        {
            return false;
        }

        if (text == Resources.File) // TODO: do not rely business logic on resource strings
        {
            return AddAtomObject(mediaType);
        }
        else
        {
            return LinkAtomObject(mediaType);
        }
    }

    private bool LinkAtomObject(string mediaType)
    {
        var index = CurrentPosition;

        if (index == -1 || index >= Count)
        {
            if (Count == 0)
            {
                return false;
            }

            index = Count - 1;
        }

        var uri = PlatformManager.Instance.AskText(Resources.InputMediaUri);

        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        try
        {
            using var change = OwnerDocument.OperationsManager.BeginComplexChange();

            if (string.IsNullOrWhiteSpace(this[index].Model.Value))
            {
                RemoveAt(index--);
            }

            var atom = new ContentItemViewModel(new ContentItem { Type = mediaType, Value = uri });
            QDocument.ActivatedObject = atom;
            Insert(index + 1, atom);
            OwnerDocument.ActiveItem = null;

            change.Commit();
            return true;
        }
        catch (Exception exc)
        {
            OwnerDocument.OnError(exc);
            return false;
        }
    }

    private void SelectAtomObject_Do(string mediaType, MediaItemViewModel file)
    {
        var index = CurrentPosition;

        if (index == -1)
        {
            index = Count - 1;
        }

        try
        {
            using var change = OwnerDocument.OperationsManager.BeginComplexChange();

            if (string.IsNullOrWhiteSpace(this[index].Model.Value))
            {
                RemoveAt(index--);
            }

            var atom = new ContentItemViewModel(new ContentItem { Type = mediaType, Value = "" });
            Insert(index + 1, atom);

            atom.Model.IsRef = true;
            atom.Model.Value = file.Model.Name;
            OwnerDocument.ActiveItem = null;

            change.Commit();
        }
        catch (Exception exc)
        {
            OwnerDocument.OnError(exc);
        }
    }

    private bool AddAtomObject(string mediaType)
    {
        QDocument document;

        try
        {
            document = OwnerDocument;
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowErrorMessage(exc.Message);
            return false;
        }

        if (document == null)
        {
            return false;
        }

        var collection = document.GetCollectionByMediaType(mediaType);
        var initialItemCount = collection.Files.Count;

        try
        {
            using var change = document.OperationsManager.BeginComplexChange();

            collection.AddItem.Execute(null);

            if (!collection.HasPendingChanges)
            {
                return false;
            }

            if (initialItemCount == collection.Files.Count)
            {
                return false;
            }

            var index = CurrentPosition;

            if (Count > 0)
            {
                if (index == -1 || index >= Count)
                {
                    if (Count == 0)
                    {
                        return false;
                    }

                    index = Count - 1;
                }

                if (string.IsNullOrWhiteSpace(this[index].Model.Value) && this[index].Model.Type != AtomTypes.Marker)
                {
                    RemoveAt(index--);
                }
            }
            else
            {
                index = -1;
            }

            var atom = new ContentItemViewModel(new ContentItem { Type = mediaType, Value = "" });
            Insert(index + 1, atom);

            var last = collection.Files.LastOrDefault();

            if (last != null)
            {
                atom.Model.IsRef = true;
                atom.Model.Value = last.Model.Name;
            }

            document.ActiveItem = null;

            change.Commit();
            return true;
        }
        catch (Exception exc)
        {
            document.OnError(exc);
            return false;
        }
    }
}
