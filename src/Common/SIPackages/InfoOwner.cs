using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SIPackages
{
    /// <summary>
    /// Владелец информации
    /// </summary>
    public abstract class InfoOwner : Named, IXmlSerializable
    {
        /// <summary>
        /// Информация об объекте
        /// </summary>
        public Info Info { get; } = new Info();

        /// <summary>
        /// Установка информации, такой же, как и у источника
        /// </summary>
        /// <param name="infoOwner">Источник информации</param>
        public void SetInfoFromOwner(InfoOwner infoOwner)
        {
            foreach (string s in infoOwner.Info.Authors)
                Info.Authors.Add(s);

            foreach (string s in infoOwner.Info.Sources)
                Info.Sources.Add(s);

            Info.Comments.Text = infoOwner.Info.Comments.Text;
        }

        /// <summary>
        /// Does the object contain specified value.
        /// </summary>
        /// <param name="value">Text value.</param>
        public override bool Contains(string value) => 
            base.Contains(value)
            || Info.Authors.ContainsQuery(value)
            || Info.Sources.ContainsQuery(value)
            || Info.Comments.Text.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1;

        /// <summary>
        /// Searches a value inside object.
        /// </summary>
        /// <param name="value">Value to search.</param>
        /// <returns>Search results.</returns>
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

        public System.Xml.Schema.XmlSchema GetSchema() => null;

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

        protected void FillInfo(InfoOwner owner)
        {
            foreach (var item in Info.Authors)
            {
                owner.Info.Authors.Add(item);
            }

            foreach (var item in Info.Sources)
            {
                owner.Info.Sources.Add(item);
            }

            owner.Info.Comments.Text = Info.Comments.Text;
            owner.Info.Extension = Info.Extension;
        }
    }
}
