using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Helpers;
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

    /// <summary>
    /// Question answer type.
    /// </summary>
    public string AnswerType
    {
        get
        {
            if (!TryGetValue(QuestionParameterNames.AnswerType, out var answerTypeParameter))
            {
                return StepParameterValues.SetAnswerTypeType_Text;
            }

            return answerTypeParameter.Model.SimpleValue;
        }
    }

    public SimpleCommand AddItem { get; }

    public SimpleCommand DeleteItem { get; }

    public SimpleCommand MakeRight { get; }

    public QuestionViewModel Owner => _question;

    public StepParametersViewModel(QuestionViewModel question, StepParameters parameters)
    {
        _question = question;
        Model = parameters;

        var isTopLevel = question.Model.Parameters == parameters;

        foreach (var parameter in parameters)
        {
            InsertSorted(new StepParameterRecord(
                parameter.Key,
                new StepParameterViewModel(question, parameter.Value, isTopLevel)));
        }

        AddItem = new SimpleCommand(AddItem_Executed);
        DeleteItem = new SimpleCommand(DeleteItem_Executed);
        MakeRight = new SimpleCommand(MakeRight_Executed);

        UpdateCommands();

        CollectionChanged += StepParametersViewModel_CollectionChanged;
    }

    internal void InsertSorted(StepParameterRecord stepParameterRecord)
    {
        var parameterWeight = GetParameterWeight(stepParameterRecord);

        for (var i = 0; i < Count; i++)
        {
            if (parameterWeight < GetParameterWeight(this[i]))
            {
                Insert(i, stepParameterRecord);
                return;
            }
        }

        Add(stepParameterRecord);
    }

    private static int GetParameterWeight(StepParameterRecord parameter)
    {
        if (parameter.Value.Model.Type == StepParameterTypes.Simple)
        {
            return -40;
        }

        if (parameter.Value.Model.Type == StepParameterTypes.NumberSet)
        {
            return -30;
        }

        if (parameter.Key == QuestionParameterNames.Question)
        {
            return -20;
        }

        if (parameter.Value.Model.Type == StepParameterTypes.Content)
        {
            return -10;
        }

        return 0;
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
        var label = IndexLabelHelper.GetIndexLabel(counter);

        var stepParameter = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new() { Type = ContentTypes.Text, Value = "" },
            }
        };

        var stepParameterViewModel = new StepParameterViewModel(question, stepParameter, false);
        InsertSorted(new StepParameterRecord(label, stepParameterViewModel));
        UpdateCommands();
    }

    private void DeleteItem_Executed(object? arg)
    {
        var item = (StepParameterRecord?)arg;
        var package = _question.OwnerTheme?.OwnerRound?.OwnerPackage;

        if (!item.HasValue || package == null)
        {
            return;
        }

        var rightAnswer = _question.Right.Count > 0 ? _question.Right[0] : "";

        using var change = package.Document.OperationsManager.BeginComplexChange();

        Remove(item.Value);

        var rightIsValid = false;

        for (var i = 0; i < Count; i++)
        {
            var key = IndexLabelHelper.GetIndexLabel(i);
            this[i] = new StepParameterRecord(key, this[i].Value);
            rightIsValid = key == rightAnswer || rightIsValid;
        }

        if (!rightIsValid && _question.Right.Count > 0 && Count > 0)
        {
            _question.Right[0] = this[^1].Key; // Set the last parameter as right answer if the previous one was removed
        }

        UpdateCommands();
        change.Commit();
    }

    private void MakeRight_Executed(object? arg)
    {
        var item = (StepParameterRecord?)arg;

        if (!item.HasValue)
        {
            return;
        }

        if (_question.Right.Count == 0)
        {
            _question.Right.Add(item.Value.Key);
        }
        else
        {
            _question.Right[0] = item.Value.Key;
        }
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
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(AnswerType)));
        UpdateCommands();
    }

    public void AddAnswer(StepParameterViewModel answer) => AddParameter(QuestionParameterNames.Answer, answer);

    public void AddParameter(string key, StepParameterViewModel parameter)
    {
        if (this.Any(p => p.Key == key))
        {
            throw new InvalidOperationException($"Key {key} alredy exist");
        }

        InsertSorted(new StepParameterRecord(key, parameter));
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
}

public record struct StepParameterRecord(string Key, StepParameterViewModel Value);
