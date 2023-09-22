using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SIPackages;

/// <summary>
/// Defines a list of package authors.
/// </summary>
[CollectionDataContract(Name = "Authors", Namespace = "")]
public sealed class AuthorInfoList : List<AuthorInfo>, IXmlSerializable
{
    /// <summary>
    /// Gets XML schema to use.
    /// </summary>
    public System.Xml.Schema.XmlSchema? GetSchema() => null;

    /// <summary>
    /// Loads data from XML reader.
    /// </summary>
    /// <param name="reader">Reader to use.</param>
    public void ReadXml(System.Xml.XmlReader reader)
    {
        AuthorInfo authorInfo = null;

        var read = true;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case System.Xml.XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "Author":
                            authorInfo = new AuthorInfo
                            {
                                Id = reader["id"]
                            };

                            Add(authorInfo);
                            break;

                        case "Name":
                            authorInfo.Name = reader.ReadElementContentAsString();
                            read = false;
                            break;

                        case "SecondName":
                            authorInfo.SecondName = reader.ReadElementContentAsString();
                            read = false;
                            break;

                        case "Surname":
                            authorInfo.Surname = reader.ReadElementContentAsString();
                            read = false;
                            break;

                        case "Country":
                            authorInfo.Country = reader.ReadElementContentAsString();
                            read = false;
                            break;

                        case "City":
                            authorInfo.City = reader.ReadElementContentAsString();
                            read = false;
                            break;
                    }

                    break;

                case System.Xml.XmlNodeType.EndElement:
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
    /// <param name="writer">Writer.</param>
    public void WriteXml(System.Xml.XmlWriter writer)
    {
        writer.WriteStartElement("Authors");

        foreach (var authorInfo in this)
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
