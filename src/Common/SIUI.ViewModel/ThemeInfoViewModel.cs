using SIUI.Model;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace SIUI.ViewModel;

/// <summary>
/// Defines theme information view model.s
/// </summary>
public sealed class ThemeInfoViewModel : ViewModelBase<ThemeInfo>
{
    /// <summary>
    /// Название темы
    /// </summary>
    public string Name
    {
        get => _model.Name;
        set { _model.Name = value; OnPropertyChanged(); }
    }

    private QuestionInfoStages _state = QuestionInfoStages.None;

    /// <summary>
    /// Состояние темы
    /// </summary>
    public QuestionInfoStages State
    {
        get => _state;
        set { _state = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Вопросы темы
    /// </summary>
    public ObservableCollection<QuestionInfoViewModel> Questions { get; } = new ObservableCollection<QuestionInfoViewModel>();

    private bool _empty = true;

    public bool Empty
    {
        get => _empty;
        set { _empty = value; OnPropertyChanged(); }
    }

    private bool _active = true;

    public bool Active
    {
        get => _active;
        set { _active = value; OnPropertyChanged(); }
    }

    public ThemeInfoViewModel()
    {
        Questions.CollectionChanged += Questions_CollectionChanged;
    }

    public ThemeInfoViewModel(ThemeInfo themeInfo) : this()
    {
        _model = themeInfo;
    }

    private void Questions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (QuestionInfoViewModel item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
                InvalidateIsEmpty();
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (QuestionInfoViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
                InvalidateIsEmpty();
                break;

            case NotifyCollectionChangedAction.Replace:
                foreach (QuestionInfoViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
                foreach (QuestionInfoViewModel item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                InvalidateIsEmpty();
                break;
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QuestionInfoViewModel.Price))
        {
            InvalidateIsEmpty();
        }
    }

    private void InvalidateIsEmpty()
    {
        Empty = !Questions.Any(questInfo => questInfo.Price > -1);
    }

    public override string ToString() => Name;

    internal async void SilentFlashOut()
    {
        try
        {
            await Task.Delay(500);

            _state = QuestionInfoStages.None;
            Name = null;
        }
        catch (Exception exc)
        {
            Trace.TraceError("SilentFlashOut error: " + exc);
        }
    }
}
