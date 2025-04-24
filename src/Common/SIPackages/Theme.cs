using SIPackages.Helpers;
using SIPackages.Models;
using SIPackages.Serializers;
using System.Xml;

namespace SIPackages;

/// <summary>
/// Represents a package theme.
/// </summary>
public sealed class Theme : InfoOwner, IEquatable<Theme>
{
    /// <summary>
    /// Theme questions.
    /// </summary>
    public List<Question> Questions { get; } = new();

    /// <inheritdoc />
    public override string ToString() => Name;

    /// <inheritdoc />
    public override void ReadXml(XmlReader reader, PackageLimits? limits = null)
    {
        Name = (reader.GetAttribute("name") ?? "").LimitLengthBy(limits?.TextLength);

        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        var read = true;

        while (!read || reader.Read())
        {
            read = true;

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "info":
                            Info.ReadXml(reader, limits);
                            read = false;
                            break;

                        case "question":
                            if (limits == null || Questions.Count < limits.QuestionCount)
                            {
                                var question = new Question();
                                question.ReadXml(reader, limits);
                                Questions.Add(question);
                            }
                            else
                            {
                                reader.Skip();
                            }

                            read = false;
                            break;
                    }

                    break;

                case XmlNodeType.EndElement:
                    if (reader.LocalName == "theme")
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
    /// <param name="writer">XML writer.</param>
    public override void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("theme");
        writer.WriteAttributeString("name", Name);
        Info.WriteXml(writer);

        if (Questions.Any())
        {
            writer.WriteStartElement("questions");

            foreach (var item in Questions)
            {
                item.WriteXml(writer);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    /// <summary>
    /// Creates a copy of this object.
    /// </summary>
    public Theme Clone()
    {
        var theme = new Theme
        {
            Name = Name
        };

        theme.SetInfoFromOwner(this);

        foreach (var quest in Questions)
        {
            theme.Questions.Add(quest.Clone());
        }

        return theme;
    }

    /// <inheritdoc />
    public bool Equals(Theme? other) => other is not null && base.Equals(other) && Questions.SequenceEqual(other.Questions);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Theme);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Questions.GetCollectionHashCode());
}
