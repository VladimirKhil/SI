using SIPackages;
using SIPackages.Core;

namespace SIQuester.ViewModel.Helpers;

internal static class ContainsExtensions
{
    /// <summary>
    /// Detects if the object contains the specified value.
    /// </summary>
    /// <param name="value">Text value.</param>
    public static bool ContainsNamed(this Named named, string value) => named.Name.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1;

    /// <inheritdoc />
    public static bool ContainsInfoOwner(this InfoOwner infoOwner, string value) =>
        ContainsNamed(infoOwner, value) ||
        infoOwner.Info.Authors.ContainsQuery(value) ||
        infoOwner.Info.Sources.ContainsQuery(value) ||
        infoOwner.Info.Comments.Text.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1 ||
        infoOwner.Info.ShowmanComments != null && infoOwner.Info.ShowmanComments.Text.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1;

    /// <summary>
    /// Checks if any of list items contains the provided pattern.
    /// </summary>
    /// <param name="list">List to check.</param>
    /// <param name="pattern">Pattern to find.</param>
    public static bool ContainsQuery(this List<string> list, string pattern) =>
        list.Any(item => item.IndexOf(pattern, StringComparison.CurrentCultureIgnoreCase) > -1);

    /// <inheritdoc />
    public static bool Contains(this Question question, string value) =>
        ContainsInfoOwner(question, value) ||
        question.Parameters.ContainsQuery(value) ||
        question.Right.ContainsQuery(value) ||
        question.Wrong.ContainsQuery(value);

    /// <summary>
    /// Does any of parameters contain specified value.
    /// </summary>
    /// <param name="value">Text value.</param>
    public static bool ContainsQuery(this StepParameters stepParameters, string value) => stepParameters.Values.Any(parameter => parameter.ContainsQuery(value));

    /// <summary>
    /// Does parameter contain specified value.
    /// </summary>
    /// <param name="value">Text value.</param>
    public static bool ContainsQuery(this StepParameter stepParameter, string value) => stepParameter.Type switch
    {
        StepParameterTypes.Content => stepParameter.ContentValue != null && stepParameter.ContentValue.Any(item => item.Value.Contains(value)),
        StepParameterTypes.Group => stepParameter.GroupValue != null && stepParameter.GroupValue.ContainsQuery(value),
        StepParameterTypes.NumberSet => stepParameter.NumberSetValue != null && stepParameter.NumberSetValue.ToString().Contains(value),
        _ => stepParameter.SimpleValue.Contains(value),
    };
}
