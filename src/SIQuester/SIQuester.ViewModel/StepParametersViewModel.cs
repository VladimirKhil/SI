using SIPackages;
using SIQuester.ViewModel.Helpers;
using System.Collections.Specialized;

namespace SIQuester.ViewModel;

/// <summary>
/// Represents step parameters view model.
/// </summary>
public sealed class StepParametersViewModel : ObservableDictionary<string, StepParameter>, INotifyCollectionChanged
{
    public StepParametersViewModel(StepParameters parameters) : base(parameters) { }
}
