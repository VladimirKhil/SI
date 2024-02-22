using SICore.BusinessLogic;
using SIPackages;

using R = SICore.Properties.Resources;

namespace SICore.Extensions;

internal static class LocalizationExtensions
{
    /// <summary>
    /// Round marker.
    /// </summary>
    private const string RoundIndicator = "@{round}";

    internal static string GetRoundName(this ILocalizer localizer, string roundName) => roundName.StartsWith(RoundIndicator)
        ? string.Format(localizer[nameof(R.RoundName)], roundName[RoundIndicator.Length..])
        : roundName;
}
