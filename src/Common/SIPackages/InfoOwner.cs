﻿using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Models;
using System.ComponentModel;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Represents an object having common information attached to it.
/// </summary>
public abstract class InfoOwner : Named
{
    private const string ShowmanCommentsTag = "showmanComments";

    /// <summary>
    /// Object information.
    /// </summary>
    [DefaultValue(typeof(Info), "")]
    public Info Info { get; } = new();

    /// <summary>
    /// Reads data from XML reader.
    /// </summary>
    /// <param name="reader">XML reader.</param>
    /// <param name="limits">Package limits.</param>
    public virtual void ReadXml(XmlReader reader, PackageLimits? limits = null)
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
                            if (limits == null || Info.Authors.Count < limits.CollectionCount)
                            {
                                Info.Authors.Add(reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength));
                                read = false;
                            }
                            break;

                        case "source":
                            if (limits == null || Info.Sources.Count < limits.CollectionCount)
                            {
                                Info.Sources.Add(reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength));
                                read = false;
                            }
                            break;

                        case "comments":
                            Info.Comments.Text = reader.ReadElementContentAsString().LimitLengthBy(limits?.TextLength);
                            read = false;
                            break;

                        case ShowmanCommentsTag:
                            Info.ShowmanComments = new Comments
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

    /// <inheritdoc />
    public virtual void WriteXml(XmlWriter writer)
    {
        var anyAuthors = Info.Authors.Any();
        var anySources = Info.Sources.Any();
        var anyComments = Info.Comments.Text.Length > 0;
        var anyShowmanComments = Info.ShowmanComments != null && Info.ShowmanComments.Text.Length > 0;

        if (anyAuthors || anySources || anyComments || anyShowmanComments)
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

            if (anyShowmanComments)
            {
                writer.WriteElementString(ShowmanCommentsTag, Info.ShowmanComments!.Text);
            }

            writer.WriteEndElement();
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is InfoOwner other && base.Equals(other) && Info.Equals(other.Info);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Info);
}
