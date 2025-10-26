using SIPackages.Core;
using SIPackages;
using SIQuester.ViewModel.Properties;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines view model for numeric answer with deviation.
/// </summary>
public sealed class NumericAnswerViewModel : ModelViewBase
{
    private readonly QuestionViewModel _question;
    private int _answer;
    private int _deviation;

    /// <summary>
    /// The numeric answer value.
    /// </summary>
    public int Answer
    {
        get => _answer;
        set
        {
            if (_answer != value)
            {
                var oldValue = _answer;
                _answer = value;
                OnPropertyChanged(oldValue);
                UpdateRightAnswer();
            }
        }
    }

    /// <summary>
    /// The allowed deviation from the answer.
    /// </summary>
    public int Deviation
    {
        get => _deviation;
        set
        {
            if (_deviation != value)
            {
                var oldValue = _deviation;
                _deviation = value;
                OnPropertyChanged(oldValue);
                UpdateDeviation();
            }
        }
    }

    public NumericAnswerViewModel(QuestionViewModel question)
    {
        _question = question;
        LoadCurrentValues();
    }

    private void LoadCurrentValues()
    {
        // Load the current answer from the right answers collection
        if (_question.Right.Count > 0)
        {
            _ = int.TryParse(_question.Right[0], out _answer);
        }

        // Load the current deviation from parameters
        if (_question.Parameters.TryGetValue(QuestionParameterNames.AnswerDeviation, out var deviationParameter))
        {
            _ = int.TryParse(deviationParameter.Model.SimpleValue, out _deviation);
        }
    }

    private void UpdateRightAnswer()
    {
        // Update the right answer in the question
        if (_question.Right.Count == 0)
        {
            _question.Right.Add(_answer.ToString());
        }
        else
        {
            _question.Right[0] = _answer.ToString();
        }
    }

    private void UpdateDeviation()
    {
        // Update the deviation parameter
        if (_question.Parameters.TryGetValue(QuestionParameterNames.AnswerDeviation, out var deviationParameter))
        {
            deviationParameter.Model.SimpleValue = _deviation.ToString();
        }
        else
        {
            // Create new deviation parameter if it doesn't exist
            var newParameter = new StepParameterViewModel(_question, new StepParameter
            {
                Type = StepParameterTypes.Simple,
                SimpleValue = _deviation.ToString()
            });

            _question.Parameters.AddParameter(QuestionParameterNames.AnswerDeviation, newParameter);
        }
    }
}