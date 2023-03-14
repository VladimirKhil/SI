using SIPackages;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a view model for question type parameter.
/// </summary>
/// <inheritdoc />
public sealed class QuestionTypeParamViewModel : ModelViewBase
{
    /// <summary>
    /// Underlying question type parameter model.
    /// </summary>
    public QuestionTypeParam Model { get; }

    public QuestionTypeViewModel Owner { get; internal set; }

    public ICommand? AddQuestionTypeParam => Owner?.AddQuestionTypeParam;

    public SimpleCommand RemoveQuestionTypeParam { get; private set; }

    public QuestionTypeParamViewModel(QuestionTypeParam item)
    {
        Model = item;
        Init();
    }

    public QuestionTypeParamViewModel()
    {
        Model = new QuestionTypeParam();
        Init();
    }

    private void Init()
    {
        RemoveQuestionTypeParam = new SimpleCommand(RemoveQuestionTypeParam_Executed);
    }

    private void RemoveQuestionTypeParam_Executed(object? arg) => Owner.Params.Remove(this);
}
