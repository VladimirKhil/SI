using SIPackages;
using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a package item view model.
/// </summary>
public abstract class ItemViewModel<T> : ModelViewBase, IItemViewModel
    where T: InfoOwner
{
    private bool _isExpanded;

    /// <summary>
    /// If item tree node is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
    }

    private bool _isSelected;

    /// <summary>
    /// If item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
    }

    private bool _isDragged;

    /// <summary>
    /// Is item is being dragged.
    /// </summary>
    public bool IsDragged
    {
        get => _isDragged;
        set { if (_isDragged != value) { _isDragged = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Add command caption.
    /// </summary>
    public virtual string AddHeader { get; } = "";

    /// <summary>
    /// Item model.
    /// </summary>
    public T Model { get; }

    /// <summary>
    /// Item common info view model.
    /// </summary>
    public InfoViewModel Info { get; private set; }

    public SimpleCommand AddAuthors { get; private set; }

    public SimpleCommand AddSources { get; private set; }

    public SimpleCommand AddComments { get; private set; }

    public ICommand SetCosts { get; private set; }

    public InfoOwner GetModel() => Model;

    public abstract IItemViewModel? Owner { get; }

    public abstract ICommand Add { get; protected set; }

    public abstract ICommand Remove { get; protected set; }

    protected ItemViewModel(T model)
    {
        Model = model;
        Info = new InfoViewModel(Model.Info, this);

        AddAuthors = new SimpleCommand(AddAuthors_Executed) { CanBeExecuted = Info.Authors.Count == 0 };
        AddSources = new SimpleCommand(AddSources_Executed) { CanBeExecuted = Info.Sources.Count == 0 };
        AddComments = new SimpleCommand(AddComments_Executed) { CanBeExecuted = Info.Comments.Text.Length == 0 };

        SetCosts = new SimpleCommand(SetCosts_Executed);

        Info.Authors.CollectionChanged += Authors_CollectionChanged;
        Info.Sources.CollectionChanged += Sources_CollectionChanged;
        Info.Comments.PropertyChanged += Comments_PropertyChanged;
    }

    private void Comments_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        AddComments.CanBeExecuted = Info.Comments.Text.Length == 0;
    }

    private void Sources_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        AddSources.CanBeExecuted = Info.Sources.Count == 0;
    }

    private void Authors_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        AddAuthors.CanBeExecuted = Info.Authors.Count == 0;
    }

    private void AddAuthors_Executed(object? arg)
    {
        QDocument.ActivatedObject = Info.Authors;
        Info.Authors.Add("");
    }

    private void AddSources_Executed(object? arg)
    {
        QDocument.ActivatedObject = Info.Sources;
        Info.Sources.Add("");
    }

    private void AddComments_Executed(object? arg)
    {
        QDocument.ActivatedObject = Info.Comments;
        Info.Comments.Text = Resources.Comment;
    }

    private void SetCosts_Executed(object? arg) => UpdateCosts((CostSetter)arg);

    protected virtual void UpdateCosts(CostSetter costSetter) { }
}
