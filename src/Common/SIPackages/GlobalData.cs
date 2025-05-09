using SIPackages.Models;
using SIPackages.Serializers;
using System.Xml;

namespace SIPackages;

// TODO: consider adding scripts section here that would allow to write common scripts for the whole package

/// <summary>
/// Contains global package data.
/// </summary>
public sealed class GlobalData
{
    /// <summary>
    /// Global authors.
    /// </summary>
    public AuthorInfoList Authors { get; } = new();

    /// <summary>
    /// Global sources.
    /// </summary>
    public SourceInfoList Sources { get; } = new();

    /// <summary>
    /// Reads data from XML reader.
    /// </summary>
    /// <param name="reader">XML reader.</param>
    /// <param name="limits">Package limits.</param>
    public void ReadXml(XmlReader reader, PackageLimits? limits)
    {
        var read = true;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "Authors":
                            Authors.ReadXml(reader, limits);
                            read = false;
                            break;

                        case "Sources":
                            Sources.ReadXml(reader, limits);
                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == "global")
                    {
                        reader.Read();
                        return;
                    }

                    break;
            }
        }
    }

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        if (Authors.Count > 0)
        {
            Authors.WriteXml(writer);
        }

        if (Sources.Count > 0)
        {
            Sources.WriteXml(writer);
        }
    }
}
