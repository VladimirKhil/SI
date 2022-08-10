using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SIPackages
{
    /// <summary>
    /// Represents an object having common information attached to it.
    /// </summary>
    public abstract class InfoOwner : Named, IXmlSerializable
    {
        /// <summary>
        /// Object information.
        /// </summary>
        public Info Info { get; } = new Info();

        /// <summary>
        /// Copies info from source object.
        /// </summary>
        /// <param name="infoOwner">Source object.</param>
        public void SetInfoFromOwner(InfoOwner infoOwner)
        {
            foreach (string s in infoOwner.Info.Authors)
            {
                Info.Authors.Add(s);
            }

            foreach (string s in infoOwner.Info.Sources)
            {
                Info.Sources.Add(s);
            }

            Info.Comments.Text = infoOwner.Info.Comments.Text;
            Info.Extension = infoOwner.Info.Extension;
        }

        /// <inheritdoc />
        public override bool Contains(string value) => 
            base.Contains(value) ||
            Info.Authors.ContainsQuery(value) ||
            Info.Sources.ContainsQuery(value) ||
            Info.Comments.Text.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1;

        /// <inheritdoc />
        public override IEnumerable<SearchData> Search(string value)
        {
            foreach (var item in base.Search(value))
            {
                yield return item;
            }

            foreach (var item in Info.Authors.Search(value))
            {
                item.Kind = ResultKind.Author;
                yield return item;
            }

            foreach (var item in Info.Sources.Search(value))
            {
                item.Kind = ResultKind.Source;
                yield return item;
            }

            foreach (var item in SearchExtensions.Search(ResultKind.Comment, Info.Comments.Text, value))
            {
                yield return item;
            }
        }

        /// <inheritdoc />
        public System.Xml.Schema.XmlSchema? GetSchema() => null;

        /// <inheritdoc />
        public virtual void ReadXml(System.Xml.XmlReader reader)
        {
            var read = true;
            while (!read || reader.Read())
            {
                read = true;
                switch (reader.NodeType)
                {
                    case System.Xml.XmlNodeType.Element:
                        switch (reader.LocalName)
                        {
                            case "author":
                                Info.Authors.Add(reader.ReadElementContentAsString());
                                read = false;
                                break;

                            case "source":
                                Info.Sources.Add(reader.ReadElementContentAsString());
                                read = false;
                                break;

                            case "comments":
                                Info.Comments.Text = reader.ReadElementContentAsString();
                                read = false;
                                break;
                        }

                        break;

                    case System.Xml.XmlNodeType.EndElement:
                        if (reader.LocalName == "info")
                        {
                            reader.Read();
                            return;
                        }
                        break;
                }
            }
        }

        /// <inheritdoc />
        public virtual void WriteXml(System.Xml.XmlWriter writer)
        {
            var anyAuthors = Info.Authors.Any();
            var anySources = Info.Sources.Any();
            var anyComments = Info.Comments.Text.Length > 0;

            if (anyAuthors || anySources || anyComments)
            {
                writer.WriteStartElement("info");

                if (anyAuthors)
                {
                    writer.WriteStartElement("authors");

                    foreach (var item in Info.Authors)
                    {
                        writer.WriteElementString("author", item);
                    }

                    writer.WriteEndElement();
                }

                if (anySources)
                {
                    writer.WriteStartElement("sources");

                    foreach (var item in Info.Sources)
                    {
                        writer.WriteElementString("source", item);
                    }

                    writer.WriteEndElement();
                }

                if (anyComments)
                {
                    writer.WriteElementString("comments", Info.Comments.Text);
                }

                writer.WriteEndElement();
            }
        }
    }
}
