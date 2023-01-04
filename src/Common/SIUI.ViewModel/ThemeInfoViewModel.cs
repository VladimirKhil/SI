using SIUI.Model;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace SIUI.ViewModel;

/// <summary>
/// Defines theme information view model.
/// </summary>
public sealed class ThemeInfoViewModel : ViewModelBase<ThemeInfo>
{
    /// <summary>
    /// Theme name.
    /// </summary>
    public string Name
    {
        get => _model.Name;
        set { _model.Name = value; OnPropertyChanged(); }
    }

    private QuestionInfoStages _state = QuestionInfoStages.None;

    /// <summary>
    /// Theme state.
    /// </summary>
    public QuestionInfoStages State
    {
        get => _state;
        set { _state = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Theme questions.
    /// </summary>
    public ObservableCollection<QuestionInfoViewModel> Questions { get; } = new();

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

    public ThemeInfoViewModel() => Questions.CollectionChanged += Questions_CollectionChanged;

    public ThemeInfoViewModel(ThemeInfo themeInfo) : this() => _model = themeInfo;

    private void Questions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null)
                {
                    break;
                }

                foreach (QuestionInfoViewModel item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }

                InvalidateIsEmpty();
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null)
                {
                    break;
                }

                foreach (QuestionInfoViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }

                InvalidateIsEmpty();
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null)
                {
                    foreach (QuestionInfoViewModel item in e.OldItems)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (QuestionInfoViewModel item in e.NewItems)
                    {
                        item.PropertyChanged += Item_PropertyChanged;
                    }
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

    private void InvalidateIsEmpty() => Empty = !Questions.Any(questInfo => questInfo.Price > QuestionInfoViewModel.InvalidPrice);

    public override string ToString() => Name;

    internal async void SilentFlashOut()
    {
        try
        {
            await Task.Delay(500);

            _state = QuestionInfoStages.None;
            Name = null; // TODO: introduce IsEnabled property
        }
        catch (Exception exc)
        {
            Trace.TraceError("SilentFlashOut error: " + exc);
        }
    }
}
