namespace SIData;

/// <summary>
/// Represents a unique package key.
/// </summary>
public sealed class PackageKey : FileKey
{
    /// <summary>
    /// Package unique identifier.
    /// </summary>
    public string? ID { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is not PackageKey other)
        {
            return base.Equals(obj);
        }

        return Name == other.Name && ID == other.ID;
    }

    public override int GetHashCode() => base.GetHashCode() * (ID == null ? -1 : ID.GetHashCode());

    public override string ToString() => $"{Name}_{BitConverter.ToString(Hash ?? Array.Empty<byte>())}_{ID}";
}
