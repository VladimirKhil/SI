using SIPackages;
using SIPackages.Core;

namespace SIQuester.ViewModel;

public sealed class StepParameterViewModel : ModelViewBase
{
    public StepParameter Model { get; }

    public ContentItemsViewModel? ContentValue { get; }

    public NumberSetEditorNewViewModel? NumberSetValue { get; }

    public StepParameterViewModel(QuestionViewModel question, StepParameter stepParameter)
    {
        Model = stepParameter;

        if (stepParameter.Type == StepParameterTypes.Content)
        {
            ContentValue = new ContentItemsViewModel(question, stepParameter.ContentValue!);
        }
        else if (stepParameter.Type == StepParameterTypes.NumberSet && stepParameter.NumberSetValue != null)
        {
            NumberSetValue = new NumberSetEditorNewViewModel(stepParameter.NumberSetValue);
        }
    }
}
