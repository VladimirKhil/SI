using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Properties;

namespace SIPackages;

/// <summary>
/// Defines a package item sources.
/// </summary>
public sealed class Sources : List<string>, IEquatable<Sources>
{
    /// <summary>
    /// Initializes a new instance of <see cref="Sources" /> class.
    /// </summary>
    public Sources() { }

    /// <summary>
    /// Initializes a new instance of <see cref="Sources" /> class.
    /// </summary>
    /// <param name="collection">Sources collection.</param>
    public Sources(IEnumerable<string> collection) : base(collection) { }

    /// <inheritdoc />
    public override string ToString() => $"{Resources.Sources}: {this.ToCommonString()}";

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Sources other && Equals(other);

    /// <inheritdoc />
    public bool Equals(Sources? other) => other is not null && this.SequenceEqual(other);

    /// <inheritdoc />
    public override int GetHashCode() => this.GetCollectionHashCode();
}
