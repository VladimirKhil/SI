using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SIPackages;

/// <summary>
/// Defines a collection of sources.
/// </summary>
[CollectionDataContract(Name = "Sources", Namespace = "")]
public sealed class SourceInfoList : List<SourceInfo>, IXmlSerializable
{
    /// <inheritdoc />
    public System.Xml.Schema.XmlSchema? GetSchema() => null;

    /// <inheritdoc />
    public void ReadXml(System.Xml.XmlReader reader)
    {
        SourceInfo? sourceInfo = null;

        var read = true;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case System.Xml.XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "Source":
                            sourceInfo = new SourceInfo
                            {
                                Id = reader["id"]
                            };

                            Add(sourceInfo);
                            break;

                        case "Author":
                            var author = reader.ReadElementContentAsString();

                            if (sourceInfo != null)
                            {
                                sourceInfo.Author = author;
                            }

                            read = false;
                            break;

                        case "Title":
                            sourceInfo.Title = reader.ReadElementContentAsString();
                            read = false;
                            break;

                        case "Year":
                            sourceInfo.Year = reader.ReadElementContentAsInt();
                            read = false;
                            break;

                        case "Publish":
                            sourceInfo.Publish = reader.ReadElementContentAsString();
                            read = false;
                            break;

                        case "City":
                            sourceInfo.City = reader.ReadElementContentAsString();
                            read = false;
                            break;
                    }

                    break;

                case System.Xml.XmlNodeType.EndElement:
                    if (reader.LocalName == "Sources")
                    {
                        reader.Read();
                        return;
                    }

                    break;
            }
        }
    }

    /// <inheritdoc />
    public void WriteXml(System.Xml.XmlWriter writer)
    {
        writer.WriteStartElement("Sources");

        foreach (var sourceInfo in this)
        {
            writer.WriteStartElement("Source");

            writer.WriteAttributeString("id", sourceInfo.Id);
            writer.WriteElementString("Author", sourceInfo.Author);
            writer.WriteElementString("Title", sourceInfo.Title);
            writer.WriteElementString("Year", sourceInfo.Year.ToString());
            writer.WriteElementString("Publish", sourceInfo.Publish);
            writer.WriteElementString("City", sourceInfo.City);

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }
}
