using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Models;
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

    /// <summary>
    /// Creates a new question in the theme.
    /// </summary>
    /// <param name="price">Question price.</param>
    /// <param name="isFinal">Does the question belong to the final round.</param>
    /// <param name="text">Question text.</param>
    public Question CreateQuestion(int price = -1, bool isFinal = false, string text = "")
    {
        int qPrice = DetectQuestionPrice(price, isFinal);

        var quest = new Question
        {
            Price = qPrice
        };

        quest.Parameters[QuestionParameterNames.Question] = new StepParameter
        {
            Type = StepParameterTypes.Content,
            ContentValue = new List<ContentItem>
            {
                new() { Type = ContentTypes.Text, Value = text },
            }
        };

        quest.Right.Add("");
        Questions.Add(quest);

        return quest;
    }

    private int DetectQuestionPrice(int price, bool isFinal)
    {
        if (price != -1)
        {
            return price;
        }

        var validQuestions = Questions.Where(q => q.Price != Question.InvalidPrice).ToList();

        var questionCount = validQuestions.Count;

        if (questionCount > 1)
        {
            var stepValue = validQuestions[1].Price - validQuestions[0].Price;
            return Math.Max(0, validQuestions[questionCount - 1].Price + stepValue);
        }

        if (questionCount > 0)
        {
            return validQuestions[0].Price * 2;
        }

        if (isFinal)
        {
            return 0;
        }

        return 100;
    }

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
                            base.ReadXml(reader, limits);
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
        base.WriteXml(writer);

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
