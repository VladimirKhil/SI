using SIPackages.Helpers;

namespace SIPackages;

/// <summary>
/// Describes a question answers collection.
/// </summary>
public sealed class Answers : List<string>, IEquatable<Answers>
{
    /// <inheritdoc />
    public bool Equals(Answers? other) => other is not null && this.SequenceEqual(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Answers);

    /// <inheritdoc />
    public override int GetHashCode() => this.GetCollectionHashCode();
}
