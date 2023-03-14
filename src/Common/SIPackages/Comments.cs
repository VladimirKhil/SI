using SIPackages.Core;
using SIPackages.Properties;
using SIPackages.TypeConverters;
using System.ComponentModel;
using System.Diagnostics;

namespace SIPackages;

/// <summary>
/// Defines a package item comments.
/// </summary>
[TypeConverter(typeof(CommentsTypeConverter))]
public sealed class Comments : PropertyChangedNotifier, IEquatable<Comments>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _text = "";

    /// <summary>
    /// Comments text.
    /// </summary>
    [DefaultValue("")]
    public string Text
    {
        get => _text;
        set
        {
            var oldValue = _text;

            if (oldValue != value)
            {
                _text = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{Resources.Comments}: {_text}";

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Comments other && Equals(other);

    /// <inheritdoc />
    public bool Equals(Comments? other) => other is not null && Text == other.Text;

    /// <inheritdoc />
    public override int GetHashCode() => Text.GetHashCode();

    /// <summary>
    /// Checks that two comments are equal to each other.
    /// </summary>
    /// <param name="left">Left comment.</param>
    /// <param name="right">Right comment.</param>
    public static bool operator ==(Comments? left, Comments? right) => Equals(left, right);

    /// <summary>
    /// Checks that two comments are not equal to each other.
    /// </summary>
    /// <param name="left">Left comment.</param>
    /// <param name="right">Right comment.</param>
    public static bool operator !=(Comments? left, Comments? right) => !(left == right);
}
