using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.TypeConverters;
using System.ComponentModel;
using System.Diagnostics;

namespace SIPackages;

/// <summary>
/// Defines a question type.
/// </summary>
[TypeConverter(typeof(QuestionTypeTypeConverter))]
public sealed class QuestionType : Named, IEquatable<QuestionType>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly List<QuestionTypeParam> _parameters = new();

    /// <summary>
    /// Type parameters specifying this type.
    /// </summary>
    public List<QuestionTypeParam> Params => _parameters;

    /// <summary>
    /// Initializes a new instance of <see cref="QuestionType" /> class.
    /// </summary>
    public QuestionType() : base(QuestionTypes.Simple) { }

    /// <summary>
    /// Gets or sets type parameter value by parameter name.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <returns>Parameter value.</returns>
    public string this[string name]
    {
        get
        {
            var item = _parameters.FirstOrDefault(qtp => qtp.Name == name);
            return item != null ? item.Value : "";
        }
        set
        {
            var item = _parameters.FirstOrDefault(qtp => qtp.Name == name);

            if (item != null)
            {
                item.Value = value;
            }
            else
            {
                _parameters.Add(new QuestionTypeParam { Name = name, Value = value });
            }
        }
    }

    /// <inheritdoc />
    public override bool Contains(string value) =>
        Name.Contains(value)
            || _parameters.Any(item => item.Name.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1
                || item.Value.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1);

    /// <inheritdoc />
    public override IEnumerable<SearchData> Search(string value)
    {
        foreach (var item in SearchExtensions.Search(ResultKind.TypeName, Name, value))
        {
            yield return item;
        }

        for (int i = 0; i < _parameters.Count; i++)
        {
            var index = _parameters[i].Name.IndexOf(value, StringComparison.CurrentCultureIgnoreCase);

            if (index > -1)
            {
                yield return new SearchData(_parameters[i].Name, index, i, ResultKind.TypeParamName);
            }

            index = _parameters[i].Value.IndexOf(value, StringComparison.CurrentCultureIgnoreCase);

            if (index > -1)
            {
                yield return new SearchData(_parameters[i].Value, index, i, ResultKind.TypeParamValue);
            }
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name}({string.Join(", ", _parameters.Select(param => param.ToString()))})";

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is QuestionType other && Equals(other);

    /// <inheritdoc />
    public bool Equals(QuestionType? other) => other is not null && base.Equals(other) && Params.SequenceEqual(other.Params);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Name, Params.GetCollectionHashCode());

    /// <summary>
    /// Checks that two question types are equal to each other.
    /// </summary>
    /// <param name="left">Left question type.</param>
    /// <param name="right">Right question type.</param>
    public static bool operator ==(QuestionType? left, QuestionType? right) => Equals(left, right);

    /// <summary>
    /// Checks that two question types are not equal to each other.
    /// </summary>
    /// <param name="left">Left question type.</param>
    /// <param name="right">Right question type.</param>
    public static bool operator !=(QuestionType? left, QuestionType? right) => !(left == right);
}
