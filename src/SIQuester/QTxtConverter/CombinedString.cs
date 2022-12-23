using Notions;

namespace QTxtConverter;

/// <summary>
/// Represents a string which is a result of combinations (intersectons) of a set of soure strings.
/// </summary>
public sealed class CombinedString
{
    private string _value;

    /// <summary>
    /// Source strings indicies in source array.
    /// </summary>
    public List<int> Sources { get; } = new();

    /// <summary>
    /// Initializes a new instance of <see cref="CombinedString" /> class.
    /// </summary>
    /// <param name="value">String value.</param>
    /// <param name="sourceIndicies">Source strings indicies in source array.</param>
    public CombinedString(string value, params int[] sourceIndicies)
    {
        _value = value;
        Sources.AddRange(sourceIndicies);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CombinedString" /> class based on other combined strings.
    /// </summary>
    /// <param name="sources">Source strings.</param>
    public CombinedString(params CombinedString[] sources)
    {
        if (sources.Length == 0)
        {
            throw new ArgumentException("sources must not be empty", nameof(sources));
        }

        _value = sources[0].ToString();

        foreach (var index in sources[0].Sources)
        {
            Sources.Add(index);
        }

        for (int i = 1; i < sources.Length; i++)
        {
            CombineWith(sources[i]);
        }
    }

    private void CombineWith(CombinedString combinedString)
    {
        var length = _value.Length;

        _value = length > 0
            ? StringManager.BestCommonSubString(
                _value,
                combinedString.ToString(),
                new StringManager.StringNorm(StringManager.TemplateSearchingNorm),
                true)
            : "";

        foreach (var index in combinedString.Sources)
        {
            if (!Sources.Contains(index))
            {
                Sources.Add(index);
            }
        }
    }

    /// <inheritdoc />
    override public string ToString() => _value;
}
