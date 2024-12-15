using SIPackages;
using SIPackages.Core;
using SIQuester.Model;
using SIQuester.ViewModel.Contracts;
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
public sealed class ContentItemsViewModel : ItemsViewModel<ContentItemViewModel>, IContentCollection
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

    /// <summary>
    /// Navigates to media file in media collection.
    /// </summary>
    public SimpleCommand NavigateToFile { get; private set; }

    public ICommand LinkFile { get; private set; }

    public SimpleCommand LinkUri { get; private set; }

    public ICommand AddFile { get; private set; }

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

        AddText = new SimpleCommand(AddScreenText_Executed);
        AddVoice = new SimpleCommand(AddReplicText_Executed);

        ChangePlacement = new SimpleCommand(ChangePlacement_Executed);

        SetTime = new SimpleCommand(SetTime_Executed);
        JoinWithNext = new SimpleCommand(JoinWithNext_Executed);

        CollapseMedia = new SimpleCommand(CollapseMedia_Executed);
        ExpandMedia = new SimpleCommand(ExpandMedia_Executed);
        ExportMedia = new SimpleCommand(ExportMedia_Executed);
        NavigateToFile = new SimpleCommand(NavigateToFile_Executed);

        LinkFile = new SimpleCommand(LinkFile_Executed);
        LinkUri = new SimpleCommand(LinkUri_Executed);
        AddFile = new SimpleCommand(AddFile_Executed);
        IsTopLevel = isTopLevel;
    }

    internal void AddScreenText_Executed(object? arg) => QDocument.ActivatedObject = Add(ContentTypes.Text, "", ContentPlacements.Screen);

    private void AddReplicText_Executed(object? arg)
    {
        var index = CurrentPosition;

        if (index > -1 && index < Count && string.IsNullOrWhiteSpace(this[index].Model.Value))
        {
            RemoveAt(index);
        }

        QDocument.ActivatedObject = Add(ContentTypes.Text, "", ContentPlacements.Replic);
    }

    private void ChangePlacement_Executed(object? arg)
    {
        var index = CurrentPosition;

        if (index > -1 && index < Count)
        {
            var contentItem = this[index];

            if (contentItem.Type != ContentTypes.Text)
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

    protected override void OnCurrentItemChanged(ContentItemViewModel? oldValue, ContentItemViewModel? newValue)
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
        var contentType = contentItem?.Model.Type;

        var isMedia = contentType == ContentTypes.Image
            || contentType == ContentTypes.Audio
            || contentType == ContentTypes.Video
            || contentType == ContentTypes.Html;

        SetTime.CanBeExecuted = contentItem != null && contentItem.Model.Duration == TimeSpan.Zero;

        CollapseMedia.CanBeExecuted = contentItem != null && isMedia && contentItem.IsExpanded;
        ExpandMedia.CanBeExecuted = contentItem != null && isMedia && !contentItem.IsExpanded;
        ExportMedia.CanBeExecuted = contentItem != null && isMedia;
        NavigateToFile.CanBeExecuted = contentItem != null && isMedia;
    }

    private void SetTime_Executed(object? arg)
    {
        if (CurrentItem == null)
        {
            return;
        }

        QDocument.ActivatedObject = CurrentItem;
        CurrentItem.Model.Duration = TimeSpan.FromSeconds(5);
    }

    private void JoinWithNext_Executed(object? arg)
    {
        if (CurrentItem == null)
        {
            return;
        }

        CurrentItem.Model.WaitForFinish = !CurrentItem.Model.WaitForFinish;
    }

    private void CollapseMedia_Executed(object? arg)
    {
        if (CurrentItem == null)
        {
            return;
        }

        CurrentItem.IsExpanded = false;
        CollapseMedia.CanBeExecuted = false;
        ExpandMedia.CanBeExecuted = true;
    }

    private void ExpandMedia_Executed(object? arg)
    {
        if (CurrentItem == null)
        {
            return;
        }

        CurrentItem.IsExpanded = true;
        CollapseMedia.CanBeExecuted = true;
        ExpandMedia.CanBeExecuted = false;
    }

    private void ExportMedia_Executed(object? arg)
    {
        if (CurrentItem == null)
        {
            return;
        }

        try
        {
            var document = OwnerDocument ?? throw new InvalidOperationException("document is undefined");
            var media = document.Document.TryGetMedia(CurrentItem.Model);

            if (media.HasValue && media.Value.HasStream)
            {
                var fileName = CurrentItem.Model.Value;

                if (PlatformManager.Instance.ShowSaveUI(Resources.ExportMedia, "", null, ref fileName))
                {
                    using var stream = media.Value.Stream!;
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

    private void NavigateToFile_Executed(object? arg)
    {
        try
        {
            var document = OwnerDocument ?? throw new InvalidOperationException("document is undefined");
            var collection = document.TryGetCollectionByMediaType(CurrentItem.Model.Type);

            if (collection == null)
            {
                return;
            }

            foreach (var file in collection.Files)
            {
                if (file.Name == CurrentItem.Model.Value)
                {
                    document.NavigateToStorageItem(collection, file);
                    return;
                }
            }
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowExclamationMessage(exc.Message);
        }
    }

    private void AddFile_Executed(object? arg)
    {
        var contentType = arg?.ToString();

        if (contentType == null)
        {
            return;
        }

        try
        {
            AddContentFile(contentType);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowExclamationMessage(exc.Message);
        }
    }

    private void LinkUri_Executed(object? arg)
    {
        var contentType = arg?.ToString();

        if (contentType == null)
        {
            return;
        }

        try
        {
            LinkContentUri(contentType);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowExclamationMessage(exc.Message);
        }
    }

    private void LinkFile_Executed(object? arg)
    {
        if (arg is not (MediaItemViewModel file, string contentType))
        {
            return;
        }

        try
        {
            LinkExistingContentFile(contentType, file);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowExclamationMessage(exc.Message);
        }
    }

    public bool SelectAtomObjectCore(object? arg)
    {
        var data = (Tuple<object, object>?)arg;
        var media = data.Item1;
        var contentType = data.Item2.ToString() ?? "";

        try
        {
            if (media is MediaItemViewModel file)
            {
                LinkExistingContentFile(contentType, file);
                return false;
            }

            if (media is not string text)
            {
                return false;
            }

            if (text == Resources.File) // TODO: do not rely business logic on resource strings
            {
                return AddContentFile(contentType);
            }
            else
            {
                return LinkContentUri(contentType);
            }
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowExclamationMessage(exc.Message);
            return false;
        }
    }

    private bool LinkContentUri(string contentType)
    {
        var document = OwnerDocument ?? throw new InvalidOperationException("document is undefined");
        var index = CurrentPosition;

        if (index == -1 || index >= Count)
        {
            index = Count - 1;
        }

        var uri = PlatformManager.Instance.AskText(Resources.InputMediaUri);

        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        try
        {
            using var change = document.OperationsManager.BeginComplexChange();

            if (index > -1 && index < Count && string.IsNullOrWhiteSpace(this[index].Model.Value))
            {
                RemoveAt(index--);
            }

            var contentItemViewModel = new ContentItemViewModel(new ContentItem
            {
                Type = contentType,
                Value = uri,
                Placement = contentType == ContentTypes.Audio ? ContentPlacements.Background : ContentPlacements.Screen,
                Duration = document.GetDurationByContentType(contentType),
            });

            QDocument.ActivatedObject = contentItemViewModel;
            Insert(index + 1, contentItemViewModel);
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

    private void LinkExistingContentFile(string contentType, MediaItemViewModel file)
    {
        var document = OwnerDocument ?? throw new InvalidOperationException("document is undefined");
        var index = CurrentPosition;

        if (index == -1)
        {
            index = Count - 1;
        }

        try
        {
            using var change = document.OperationsManager.BeginComplexChange();

            if (string.IsNullOrWhiteSpace(this[index].Model.Value))
            {
                RemoveAt(index--);
            }

            var contentItemViewModel = new ContentItemViewModel(new ContentItem
            { 
                Type = contentType,
                Value = "",
                Placement = contentType == ContentTypes.Audio ? ContentPlacements.Background : ContentPlacements.Screen,
                Duration = document.GetDurationByContentType(contentType),
            });

            Insert(index + 1, contentItemViewModel);

            contentItemViewModel.Model.IsRef = true;
            contentItemViewModel.Model.Value = file.Model.Name;
            document.ActiveItem = null;

            change.Commit();
        }
        catch (Exception exc)
        {
            document.OnError(exc);
        }
    }

    private bool AddContentFile(string contentType)
    {
        QDocument? document;

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

        var collection = document.GetCollectionByMediaType(contentType);
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

                if (string.IsNullOrWhiteSpace(this[index].Model.Value))
                {
                    RemoveAt(index--);
                }
            }
            else
            {
                index = -1;
            }

            for (var i = initialItemCount; i < collection.Files.Count; i++)
            {
                var contentItemViewModel = new ContentItemViewModel(new ContentItem
                {
                    Type = contentType,
                    Value = "",
                    Placement = contentType == ContentTypes.Audio ? ContentPlacements.Background : ContentPlacements.Screen,
                    Duration = document.GetDurationByContentType(contentType),
                });

                Insert(index + 1 + (i - initialItemCount), contentItemViewModel);

                var file = collection.Files[i];

                contentItemViewModel.Model.IsRef = true;
                contentItemViewModel.Model.Value = file.Model.Name;

                if (AppSettings.Default.SetRightAnswerFromFileName)
                {
                    var question = Owner;

                    if (question.Right.Last().Length == 0)
                    {
                        question.Right.RemoveAt(question.Right.Count - 1);
                    }

                    question.Right.Add(Path.GetFileNameWithoutExtension(file.Model.Name));
                }
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
