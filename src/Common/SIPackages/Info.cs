using SIPackages.TypeConverters;
using System.ComponentModel;

namespace SIPackages;

/// <summary>
/// Contains package item information.
/// </summary>
[TypeConverter(typeof(InfoTypeConverter))]
public sealed class Info : IEquatable<Info>
{
    /// <summary>
    /// Item authors.
    /// </summary>
    public Authors Authors { get; } = new();

    /// <summary>
    /// Item sources.
    /// </summary>
    public Sources Sources { get; } = new();

    /// <summary>
    /// Item comments.
    /// </summary>
    [DefaultValue(typeof(Comments), "")]
    public Comments Comments { get; } = new();

    /// <summary>
    /// Item extension data.
    /// </summary>
    public string? Extension { get; set; }

    /// <inheritdoc />
    public override string ToString() => string.Format("[{0}, {1}, {2}]", Authors, Sources, Comments);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Info other && Equals(other);

    /// <inheritdoc />
    public bool Equals(Info? other) =>
        other is not null
        && Authors.Equals(other.Authors)
        && Sources.Equals(other.Sources)
        && Comments.Equals(other.Comments);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Authors, Sources, Comments);

    /// <summary>
    /// Checks that two infos are equal to each other.
    /// </summary>
    /// <param name="left">Left info.</param>
    /// <param name="right">Right info.</param>
    public static bool operator ==(Info left, Info right) => left.Equals(right);

    /// <summary>
    /// Checks that two infos are not equal to each other.
    /// </summary>
    /// <param name="left">Left info.</param>
    /// <param name="right">Right info.</param>
    public static bool operator !=(Info left, Info right) => !(left == right);
}
