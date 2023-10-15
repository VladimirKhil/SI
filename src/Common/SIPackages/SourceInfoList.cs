using SIPackages.Helpers;
using SIPackages.Models;
using System.Runtime.Serialization;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Defines a collection of sources.
/// </summary>
[CollectionDataContract(Name = "Sources", Namespace = "")]
public sealed class SourceInfoList : List<SourceInfo>
{
    /// <summary>
    /// Loads data from XML reader.
    /// </summary>
    /// <param name="reader">Reader to use.</param>
    /// <param name="limits">Package limits.</param>
    public void ReadXml(XmlReader reader, PackageLimits? limits = null)
    {
        SourceInfo? sourceInfo = null;

        var read = true;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "Source":
                            if (limits == null || Count < limits.CollectionCount)
                            {
                                sourceInfo = new SourceInfo
                                {
                                    Id = reader["id"]
                                };

                                Add(sourceInfo);
                            }
                            else
                            {
                                reader.Skip();
                            }
                            break;

                        case "Author":
                            var author = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);

                            if (sourceInfo != null)
                            {
                                sourceInfo.Author = author;
                            }

                            read = false;
                            break;

                        case "Title":
                            sourceInfo.Title = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;

                        case "Year":
                            sourceInfo.Year = reader.ReadElementContentAsInt();
                            read = false;
                            break;

                        case "Publish":
                            sourceInfo.Publish = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;

                        case "City":
                            sourceInfo.City = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
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
    public void WriteXml(XmlWriter writer)
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
