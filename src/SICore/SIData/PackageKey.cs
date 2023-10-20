namespace SIData;

/// <summary>
/// Represents a unique package key.
/// </summary>
public sealed class PackageKey : FileKey
{
    /// <summary>
    /// Package unique identifier.
    /// </summary>
    [Obsolete("For backward compatibility")]
    public string? ID { get; set; }

    /// <summary>
    /// Package uri.
    /// </summary>
    public Uri? Uri { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is not PackageKey other)
        {
            return base.Equals(obj);
        }

        return Name == other.Name && Uri == other.Uri;
    }

    public override int GetHashCode() => base.GetHashCode() * (Uri == null ? -1 : Uri.GetHashCode());

    public override string ToString() => $"{Name}_{BitConverter.ToString(Hash ?? Array.Empty<byte>())}_{Uri}";
}
