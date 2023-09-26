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
/// Defines a question scenario view model.
/// </summary>
public sealed class ScenarioViewModel : ItemsViewModel<AtomViewModel>
{
    internal Scenario Model { get; }

    internal QuestionViewModel Owner { get; private set; }

    public ICommand AddText { get; private set; }

    public ICommand AddVoice { get; private set; }

    public ICommand AddMarker { get; private set; }

    public ICommand ChangeType { get; private set; }

    public SimpleCommand SetTime { get; private set; }

    public SimpleCommand CollapseMedia { get; private set; }

    public SimpleCommand ExpandMedia { get; private set; }

    public SimpleCommand ExportMedia { get; private set; }

    public ICommand SelectAtomObject { get; private set; }

    public override QDocument? OwnerDocument => Owner.OwnerTheme?.OwnerRound?.OwnerPackage?.Document;

    private bool _isComplex;

    /// <summary>
    /// Does this scenario contain a complex answer.
    /// </summary>
    public bool IsComplex
    {
        get => _isComplex;
        set
        {
            if (_isComplex != value)
            {
                _isComplex = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsComplex)));
            }
        }
    }

    public ScenarioViewModel(QuestionViewModel owner, Scenario scenario)
    {
        Model = scenario;
        Owner = owner;

        foreach (var atom in scenario)
        {
            Add(new AtomViewModel(atom) { OwnerScenario = this });

            if (atom.Type == AtomTypes.Marker)
            {
                IsComplex = true;
            }
        }

        CollectionChanged += ScenarioViewModel_CollectionChanged;

        AddText = new SimpleCommand(AddText_Executed);
        AddVoice = new SimpleCommand(AddVoice_Executed);
        AddMarker = new SimpleCommand(AddMarker_Executed);

        ChangeType = new SimpleCommand(ChangeType_Executed);

        SetTime = new SimpleCommand(SetTime_Executed);

        CollapseMedia = new SimpleCommand(CollapseMedia_Executed);
        ExpandMedia = new SimpleCommand(ExpandMedia_Executed);
        ExportMedia = new SimpleCommand(ExportMedia_Executed);

        SelectAtomObject = new SimpleCommand(SelectAtomObject_Executed);

        UpdateCommands();
    }

    protected override void OnCurrentItemChanged(AtomViewModel oldValue, AtomViewModel newValue)
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

        UpdateAtomCommands();
    }

    private void CurrentAtom_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AtomViewModel.IsExpanded))
        {
            UpdateAtomCommands();
        }
    }

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Atom.AtomTime))
        {
            UpdateAtomCommands();
        }
    }

    private void UpdateAtomCommands()
    {
        var atom = CurrentItem;
        var atomType = atom.Model.Type;
        var isMedia = atomType == AtomTypes.Image || atomType == AtomTypes.Audio || atomType == AtomTypes.Video;

        SetTime.CanBeExecuted = atom != null && atom.Model.AtomTime == 0;

        CollapseMedia.CanBeExecuted = atom != null && isMedia && atom.IsExpanded;
        ExpandMedia.CanBeExecuted = atom != null && isMedia && !atom.IsExpanded;
        ExportMedia.CanBeExecuted = atom != null && isMedia;
    }

    private void ScenarioViewModel_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems?.Count; i++)
                {
                    this[i].OwnerScenario = this;
                    Model.Insert(i, this[i].Model);
                }

                break;

            case NotifyCollectionChangedAction.Replace:
                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems?.Count; i++)
                {
                    this[i].OwnerScenario = this;
                    Model[i] = this[i].Model;
                }

                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (var atom in e.OldItems.Cast<AtomViewModel>())
                    {
                        atom.OwnerScenario = null;
                        Model.RemoveAt(e.OldStartingIndex);
                        OwnerDocument?.ClearLinks(atom);
                    }
                }

                IsComplex = Items.Any(atom => atom.Model.Type == AtomTypes.Marker);
                break;

            case NotifyCollectionChangedAction.Reset:
                Model.Clear();

                foreach (AtomViewModel atom in this)
                {
                    atom.OwnerScenario = this;
                    Model.Add(atom.Model);
                }

                IsComplex = Items.Any(atom => atom.Model.Type == AtomTypes.Marker);
                break;
        }

        UpdateCommands();
    }

    internal AtomViewModel Add(string atomType, string atomText)
    {
        var atom = new AtomViewModel(new Atom { Type = atomType, Text = atomText });
        Add(atom);

        return atom;
    }

    internal void SelectAtomObject_AsAnswer(object arg) => SelectAtomObjectCore(arg, true);

    internal void AddText_Executed(object? arg) => QDocument.ActivatedObject = Add(AtomTypes.Text, "");

    private void AddVoice_Executed(object? arg)
    {
        var index = CurrentPosition;

        if (index > -1 && index < Count && string.IsNullOrWhiteSpace(this[index].Model.Text))
        {
            RemoveAt(index);
        }

        QDocument.ActivatedObject = Add(AtomTypes.Oral, "");
    }

    internal void AddMarker_Executed(object? arg)
    {
        Add(AtomTypes.Marker, "");
        IsComplex = true;
    }

    private void ChangeType_Executed(object? arg)
    {
        var index = CurrentPosition;

        if (index > -1 && index < Count)
        {
            var atom = this[index];

            if (atom.Model.Type == AtomTypes.Text)
            {
                atom.Model.Type = AtomTypes.Oral;
            }
            else if (atom.Model.Type == AtomTypes.Oral)
            {
                atom.Model.Type = AtomTypes.Text;
            }
        }
    }

    protected override bool CanRemove() => Count > 1;

    private void SetTime_Executed(object? arg)
    {
        QDocument.ActivatedObject = CurrentItem;
        CurrentItem.Model.AtomTime = 5;
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
            var document = OwnerDocument ?? throw new InvalidOperationException("document is undefined");
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

    private void SelectAtomObject_Executed(object? arg) => SelectAtomObjectCore(arg, false);

    private void SelectAtomObjectCore(object? arg, bool asAnswer)
    {
        var data = (Tuple<object, object>)arg;
        var media = data.Item1;
        var mediaType = data.Item2.ToString();

        if (media is MediaItemViewModel file)
        {
            SelectAtomObject_Do(mediaType, file, asAnswer);
            return;
        }

        if (mediaType == AtomTypes.Html)
        {
            LinkAtomObject(mediaType, asAnswer);
            return;
        }

        if (media is not string text)
        {
            return;
        }

        if (text == Resources.File) // TODO: do not rely business logic on resource strings
        {
            AddAtomObject(mediaType, asAnswer);
        }
        else
        {
            LinkAtomObject(mediaType, asAnswer);
        }
    }

    private void LinkAtomObject(string mediaType, bool asAnswer)
    {
        var index = CurrentPosition;

        if (index == -1 || index >= Count)
        {
            if (Count == 0)
            {
                return;
            }

            index = Count - 1;
        }

        var uri = PlatformManager.Instance.AskText(Resources.InputMediaUri);

        if (string.IsNullOrWhiteSpace(uri))
        {
            return;
        }

        try
        {
            using var change = OwnerDocument.OperationsManager.BeginComplexChange();

            if (asAnswer)
            {
                if (!_isComplex)
                {
                    AddMarker_Executed(null);
                    index = Count - 1;
                }
            }
            else if (string.IsNullOrWhiteSpace(this[index].Model.Text))
            {
                RemoveAt(index--);
            }

            var atom = new AtomViewModel(new Atom { Type = mediaType, Text = uri });
            QDocument.ActivatedObject = atom;
            Insert(index + 1, atom);
            OwnerDocument.ActiveItem = null;

            change.Commit();
        }
        catch (Exception exc)
        {
            OwnerDocument.OnError(exc);
        }
    }

    private void SelectAtomObject_Do(string mediaType, MediaItemViewModel file, bool asAnswer)
    {
        var index = CurrentPosition;

        if (index == -1)
        {
            index = Count - 1;
        }

        try
        {
            using var change = OwnerDocument.OperationsManager.BeginComplexChange();

            if (asAnswer)
            {
                if (!_isComplex)
                {
                    AddMarker_Executed(null);
                    index = Count - 1;
                }
            }
            else if (string.IsNullOrWhiteSpace(this[index].Model.Text))
            {
                RemoveAt(index--);
            }

            var atom = new AtomViewModel(new Atom { Type = mediaType, Text = "" });
            Insert(index + 1, atom);

            SIDocument.SetLink(atom.Model, file.Model.Name);
            OwnerDocument.ActiveItem = null;

            change.Commit();
        }
        catch (Exception exc)
        {
            OwnerDocument.OnError(exc);
        }
    }

    private void AddAtomObject(string mediaType, bool asAnswer)
    {
        QDocument document;

        try
        {
            document = OwnerDocument;
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowErrorMessage(exc.Message);
            return;
        }

        if (document == null)
        {
            return;
        }

        var collection = document.GetCollectionByMediaType(mediaType);
        var initialItemCount = collection.Files.Count;

        try
        {
            using var change = document.OperationsManager.BeginComplexChange();

            collection.AddItem.Execute(null);

            if (!collection.HasPendingChanges)
            {
                return;
            }

            if (initialItemCount == collection.Files.Count)
            {
                return;
            }

            var index = CurrentPosition;

            if (index == -1 || index >= Count)
            {
                if (Count == 0)
                {
                    return;
                }

                index = Count - 1;
            }

            if (asAnswer)
            {
                if (!_isComplex)
                {
                    AddMarker_Executed(null);
                    index = Count - 1;
                }
            }
            else if (string.IsNullOrWhiteSpace(this[index].Model.Text) && this[index].Model.Type != AtomTypes.Marker)
            {
                RemoveAt(index--);
            }

            var atom = new AtomViewModel(new Atom { Type = mediaType, Text = "" });
            Insert(index + 1, atom);

            var last = collection.Files.LastOrDefault();

            if (last != null)
            {
                SIDocument.SetLink(atom.Model, last.Model.Name);
            }

            document.ActiveItem = null;

            change.Commit();
        }
        catch (Exception exc)
        {
            document.OnError(exc);
        }
    }
}
