using SIPackages.Core;
using System.Diagnostics;

namespace SIPackages;

/// <summary>
/// Defines a question type parameter.
/// </summary>
public sealed class QuestionTypeParam : Named, IEquatable<QuestionTypeParam>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _value = "";

    /// <summary>
    /// Parameter value.
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            var oldValue = _value;

            if (oldValue != value)
            {
                _value = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name}: {Value}";

    /// <inheritdoc />
    public bool Equals(QuestionTypeParam? other) => other is not null && base.Equals(other) && Value.Equals(other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as QuestionTypeParam);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), _value);
}
