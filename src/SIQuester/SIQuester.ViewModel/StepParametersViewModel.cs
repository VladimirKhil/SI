using SIPackages;
using SIPackages.Core;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents step parameters view model.
/// </summary>
public sealed class StepParametersViewModel : ObservableCollection<KeyValuePair<string, StepParameterViewModel>>
{
    public StepParameters Model { get; }

    public bool HasComplexAnswer => Model.ContainsKey(QuestionParameterNames.Answer);

    public StepParametersViewModel(QuestionViewModel question, StepParameters parameters)
    {
        Model = parameters;

        foreach (var parameter in parameters)
        {
            Add(new KeyValuePair<string, StepParameterViewModel>(parameter.Key, new StepParameterViewModel(question, parameter.Value)));
        }

        CollectionChanged += StepParametersViewModel_CollectionChanged;
    }

    private void StepParametersViewModel_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems?.Count; i++)
                {
                    var item = this[i];
                    Model[item.Key] = item.Value.Model;
                }

                break;

            case NotifyCollectionChangedAction.Replace:
                for (int i = e.NewStartingIndex; i < e.NewStartingIndex + e.NewItems?.Count; i++)
                {
                    var item = this[i];
                    Model[item.Key] = item.Value.Model;
                }

                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.Cast<KeyValuePair<string, StepParameterViewModel>>())
                    {
                        Model.Remove(item.Key);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                Model.Clear();
                break;
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasComplexAnswer)));
    }

    public void AddAnswer(StepParameterViewModel answer) => Add(new KeyValuePair<string, StepParameterViewModel>(QuestionParameterNames.Answer, answer));

    internal void RemoveAnswer()
    {
        for (var i = 0; i < Count; i++)
        {
            if (this[i].Key == QuestionParameterNames.Answer)
            {
                RemoveAt(i);
                break;
            }
        }
    }
}
