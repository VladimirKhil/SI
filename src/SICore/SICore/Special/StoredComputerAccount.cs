using SIData;

namespace SICore;

public sealed class StoredComputerAccount : ComputerAccount
{
    private const string UnsetName = "#";

    private const string DefaultCulture = "en";

    public new bool IsMale { get; set; }

    public IDictionary<string, string> Names { get; set; }

    public string GetLocalizedName(string cultureCode)
    {
        if (Names.TryGetValue(cultureCode, out var name))
        {
            return name;
        }

        if (Names.TryGetValue(DefaultCulture, out name))
        {
            return name;
        }

        if (Names.Any())
        {
            return Names.First().Value;
        }

        return UnsetName + Random.Shared.Next(short.MaxValue);
    }
}
