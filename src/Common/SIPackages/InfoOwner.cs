using SIPackages.Core;
using SIPackages.Models;
using System.ComponentModel;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Represents an object having common information attached to it.
/// </summary>
public abstract class InfoOwner : Named
{
    /// <summary>
    /// Object information.
    /// </summary>
    [DefaultValue(typeof(Info), "")]
    public Info Info { get; } = new();

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is InfoOwner other && base.Equals(other) && Info.Equals(other.Info);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Info);

    /// <summary>
    /// Reads data from XML reader.
    /// </summary>
    public abstract void ReadXml(XmlReader reader, PackageLimits? limits = null);

    /// <summary>
    /// Writes data to XML writer.
    /// </summary>
    public abstract void WriteXml(XmlWriter writer);
}
