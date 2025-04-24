using SIPackages.Helpers;
using SIPackages.Models;
using System.Xml;

namespace SIPackages.Serializers;

/// <summary>
/// Provides methods for serializing and deserializing <see cref="Info" />.
/// </summary>
public static class InfoSerializer
{
    private const string ShowmanCommentsTag = "showmanComments";

    /// <summary>
    /// Reads info from XML reader.
    /// </summary>
    /// <param name="info">Info object.</param>
    /// <param name="reader">XML reader.</param>
    /// <param name="limits">Package limits.</param>
    public static void ReadXml(this Info info, XmlReader reader, PackageLimits? limits = null)
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
                        case "author":
                            if (limits == null || info.Authors.Count < limits.CollectionCount)
                            {
                                info.Authors.Add(reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength));
                                read = false;
                            }
                            break;

                        case "source":
                            if (limits == null || info.Sources.Count < limits.CollectionCount)
                            {
                                info.Sources.Add(reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength));
                                read = false;
                            }
                            break;

                        case "comments":
                            info.Comments.Text = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;

                        case ShowmanCommentsTag:
                            info.ShowmanComments = new Comments
                            {
                                Text = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength)
                            };

                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == "info")
                    {
                        reader.Read();
                        return;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Writes info to XML writer.
    /// </summary>
    /// <param name="info">Info object.</param>
    /// <param name="writer">XML writer.</param>
    public static void WriteXml(this Info info, XmlWriter writer)
    {
        var anyAuthors = info.Authors.Any();
        var anySources = info.Sources.Any();
        var anyComments = info.Comments.Text.Length > 0;
        var anyShowmanComments = info.ShowmanComments != null && info.ShowmanComments.Text.Length > 0;

        if (anyAuthors || anySources || anyComments || anyShowmanComments)
        {
            writer.WriteStartElement("info");

            if (anyAuthors)
            {
                writer.WriteStartElement("authors");

                foreach (var item in info.Authors)
                {
                    writer.WriteElementString("author", item);
                }

                writer.WriteEndElement();
            }

            if (anySources)
            {
                writer.WriteStartElement("sources");

                foreach (var item in info.Sources)
                {
                    writer.WriteElementString("source", item);
                }

                writer.WriteEndElement();
            }

            if (anyComments)
            {
                writer.WriteElementString("comments", info.Comments.Text);
            }

            if (anyShowmanComments)
            {
                writer.WriteElementString(ShowmanCommentsTag, info.ShowmanComments!.Text);
            }

            writer.WriteEndElement();
        }
    }
}
