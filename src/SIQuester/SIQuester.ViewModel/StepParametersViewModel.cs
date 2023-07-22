using SIPackages;
using SIQuester.ViewModel.Helpers;
using System.Collections.Specialized;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents step parameters view model.
/// </summary>
public sealed class StepParametersViewModel : ObservableDictionary<string, StepParameterViewModel>, INotifyCollectionChanged
{
    public StepParametersViewModel(QuestionViewModel question, StepParameters parameters)
    {
        foreach (var parameter in parameters)
        {
            this[parameter.Key] = new StepParameterViewModel(question, parameter.Value);
        }
    }
}
