using SIPackages.Helpers;
using SIPackages.Models;
using System.Xml;

namespace SIPackages.Serializers;

internal static class AuthorInfoListSerializer
{
    /// <summary>
    /// Loads data from XML reader.
    /// </summary>
    /// <param name="authorInfos">List of authors.</param>
    /// <param name="reader">Reader to use.</param>
    /// <param name="limits">Package limits.</param>
    public static void ReadXml(this AuthorInfoList authorInfos, XmlReader reader, PackageLimits? limits = null)
    {
        AuthorInfo? authorInfo = null;

        var read = true;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "Author":
                            if (limits == null || authorInfos.Count < limits.CollectionCount)
                            {
                                authorInfo = new AuthorInfo
                                {
                                    Id = reader["id"] ?? ""
                                };

                                authorInfos.Add(authorInfo);
                            }
                            else
                            {
                                reader.Skip();
                            }
                            break;

                        case "Name":
                            authorInfo.Name = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;

                        case "SecondName":
                            authorInfo.SecondName = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;

                        case "Surname":
                            authorInfo.Surname = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;

                        case "Country":
                            authorInfo.Country = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;

                        case "City":
                            authorInfo.City = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == "Authors")
                    {
                        reader.Read();
                        return;
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Writes data to XML writer.
    /// </summary>
    /// <param name="authorInfos">List of authors.</param>
    /// <param name="writer">Writer.</param>
    public static void WriteXml(this AuthorInfoList authorInfos, XmlWriter writer)
    {
        writer.WriteStartElement("Authors");

        foreach (var authorInfo in authorInfos)
        {
            writer.WriteStartElement("Author");

            writer.WriteAttributeString("id", authorInfo.Id);
            writer.WriteElementString("Name", authorInfo.Name);
            writer.WriteElementString("SecondName", authorInfo.SecondName);
            writer.WriteElementString("Surname", authorInfo.Surname);
            writer.WriteElementString("Country", authorInfo.Country);
            writer.WriteElementString("City", authorInfo.City);

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }
}
