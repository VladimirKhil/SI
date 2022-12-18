using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Properties;

namespace SIPackages;

/// <summary>
/// Defines a list of package object authors names.
/// </summary>
public sealed class Authors : List<string>, IEquatable<Authors>
{
    /// <summary>
    /// Initializes a new instance of <see cref="Authors" /> class.
    /// </summary>
    public Authors() { }

    /// <summary>
    /// Initializes a new instance of <see cref="Authors" /> class.
    /// </summary>
    /// <param name="collection">Initial authors names collection.</param>
    public Authors(IList<string> collection) : base(collection) { }

    /// <inheritdoc />
    public override string ToString() => $"{Resources.Authors}: {this.ToCommonString()}";

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Authors other && Equals(other);

    /// <inheritdoc />
    public bool Equals(Authors? other) => other is not null && this.SequenceEqual(other);

    /// <inheritdoc />
    public override int GetHashCode() => this.GetCollectionHashCode();
}
