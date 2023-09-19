using SIPackages;
using SIPackages.Core;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents step parameters view model.
/// </summary>
public sealed class StepParametersViewModel : ObservableCollection<StepParameterRecord>
{
    private readonly QuestionViewModel _question;

    public StepParameters Model { get; }

    public bool HasComplexAnswer => Model.ContainsKey(QuestionParameterNames.Answer);

    public SimpleCommand AddItem { get; private set; }

    public SimpleCommand DeleteItem { get; private set; }

    public StepParametersViewModel(QuestionViewModel question, StepParameters parameters)
    {
        _question = question;
        Model = parameters;

        var isTopLevel = question.Model.Parameters == parameters;

        foreach (var parameter in parameters)
        {
            Add(new StepParameterRecord(
                parameter.Key,
                new StepParameterViewModel(question, parameter.Value, isTopLevel)));
        }

        AddItem = new SimpleCommand(AddItem_Executed);
        DeleteItem = new SimpleCommand(DeleteItem_Executed);

        UpdateCommands();

        CollectionChanged += StepParametersViewModel_CollectionChanged;
    }

    private void UpdateCommands()
    {
        DeleteItem.CanBeExecuted = Count > 2; // TODO: this is a forced mode for select options. That is not true for other cases
    }

    private void AddItem_Executed(object? arg)
    {
        if (arg is not QuestionViewModel question)
        {
            return;
        }

        var counter = Count;
        var label = GetIndexLabel(counter);

        var stepParameter = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new ContentItem { Type = AtomTypes.Text, Value = "" },
            }
        };

        var stepParameterViewModel = new StepParameterViewModel(question, stepParameter, false);
        Add(new StepParameterRecord(label, stepParameterViewModel));
        UpdateCommands();
    }

    private void DeleteItem_Executed(object? arg)
    {
        var item = (StepParameterRecord?)arg;

        if (!item.HasValue || _question.OwnerTheme?.OwnerRound?.OwnerPackage == null)
        {
            return;
        }

        using var change = _question.OwnerTheme.OwnerRound.OwnerPackage.Document.OperationsManager.BeginComplexChange();

        Remove(item.Value);

        for (int i = 0; i < Count; i++)
        {
            this[i] = new StepParameterRecord(GetIndexLabel(i), this[i].Value);
        }

        UpdateCommands();
        change.Commit();
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
                    foreach (var item in e.OldItems.Cast<StepParameterRecord>())
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
        UpdateCommands();
    }

    public void AddAnswer(StepParameterViewModel answer) => AddParameter(QuestionParameterNames.Answer, answer);

    public void AddParameter(string key, StepParameterViewModel parameter)
    {
        if (this.Any(p => p.Key == key))
        {
            throw new InvalidOperationException($"Key {key} alredy exist");
        }

        Add(new StepParameterRecord(key, parameter));
    }

    internal void RemoveAnswer() => RemoveParameter(QuestionParameterNames.Answer);

    internal bool RemoveParameter(string parameterName)
    {
        for (var i = 0; i < Count; i++)
        {
            if (this[i].Key == parameterName)
            {
                RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    internal bool TryGetValue(string key, [NotNullWhen(true)] out StepParameterViewModel? parameter)
    {
        foreach (var item in this)
        {
            if (item.Key == key)
            {
                parameter = item.Value;
                return true;
            }
        }

        parameter = null;
        return false;
    }

    private static string GetIndexLabel(int index) => index < 26 ? ((char)('A' + index)).ToString() : 'A' + (index - 25).ToString();
}

public record struct StepParameterRecord(string Key, StepParameterViewModel Value);
