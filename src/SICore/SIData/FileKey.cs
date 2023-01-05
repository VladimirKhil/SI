namespace SIData;

/// <summary>
/// Defines a file unique key.
/// </summary>
public class FileKey
{
    /// <summary>
    /// File name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// File hash.
    /// </summary>
    public byte[] Hash { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is not FileKey other)
        {
            return base.Equals(obj);
        }

        return Name == other.Name && (Hash == null && other.Hash == null || Hash.SequenceEqual(other.Hash));
    }

    public override int GetHashCode() =>
        Hash != null ? Convert.ToBase64String(Hash).GetHashCode() : (Name != null ? Name.GetHashCode() : -1);

    public override string ToString() => $"{Convert.ToBase64String(Hash)}_{Name}";

    public static FileKey Parse(string s)
    {
        var index = s.IndexOf('_');

        if (index == -1)
        {
            throw new InvalidCastException();
        }

        return new FileKey { Name = s[(index + 1)..], Hash = Convert.FromBase64String(s[..index]) };
    }
}
