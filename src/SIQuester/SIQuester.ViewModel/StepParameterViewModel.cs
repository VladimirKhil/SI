using SIPackages;
using SIPackages.Core;

namespace SIQuester.ViewModel;

public sealed class StepParameterViewModel : ModelViewBase
{
    public StepParameter Model { get; }

    public ContentItemsViewModel? ContentValue { get; }

    public StepParameterViewModel(QuestionViewModel question, StepParameter stepParameter)
    {
        Model = stepParameter;

        if (stepParameter.Type == StepParameterTypes.Content)
        {
            ContentValue = new ContentItemsViewModel(question, stepParameter.ContentValue!);
        }
    }
}
