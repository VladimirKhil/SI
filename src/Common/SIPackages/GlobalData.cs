using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SIPackages;

// TODO: consider adding scripts section here that would allow to write common scripts for the whole package

/// <summary>
/// Contains global package data.
/// </summary>
public sealed class GlobalData : IXmlSerializable
{
    /// <summary>
    /// Global authors.
    /// </summary>
    public AuthorInfoList? Authors { get; set; }

    /// <summary>
    /// Global sources.
    /// </summary>
    public SourceInfoList? Sources { get; set; }

    /// <inheritdoc />
    public XmlSchema? GetSchema() => null;

    /// <inheritdoc />
    public void ReadXml(XmlReader reader)
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
                            Authors = new AuthorInfoList();
                            Authors.ReadXml(reader);
                            read = false;
                            break;

                        case "Sources":
                            Sources = new SourceInfoList();
                            Sources.ReadXml(reader);
                            read = false;
                            break;
                    }

                    break;
            }
        }
    }

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        Authors?.WriteXml(writer);
        Sources?.WriteXml(writer);
    }
}
