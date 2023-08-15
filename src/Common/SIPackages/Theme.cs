using SIPackages.Core;
using SIPackages.Helpers;
using SIPackages.Properties;

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
    public override string ToString() => $"{Resources.Theme}: {Name}";

    /// <summary>
    /// Creates a new question in the theme.
    /// </summary>
    /// <param name="price">Question price.</param>
    /// <param name="isFinal">Does the question belong to the final round.</param>
    /// <param name="upgraded">Does the theme belong to an upgraded package.</param>
    public Question CreateQuestion(int price = -1, bool isFinal = false, bool upgraded = false)
    {
        int qPrice = DetectQuestionPrice(price, isFinal);

        var quest = new Question
        {
            Price = qPrice
        };

        if (upgraded)
        {
            quest.Parameters = new StepParameters
            {
                [QuestionParameterNames.Question] = new StepParameter
                {
                    Type = StepParameterTypes.Content,
                    ContentValue = new List<ContentItem>
                    {
                        new ContentItem { Type = AtomTypes.Text, Value = "" },
                    }
                }
            };
        }
        else
        {
            quest.Scenario.Add(new Atom());
        }

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

    /// <summary>
    /// Reades data from XML reader.
    /// </summary>
    /// <param name="reader">XML Reader.</param>
    public override void ReadXml(System.Xml.XmlReader reader)
    {
        Name = reader.GetAttribute("name") ?? "";

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
                case System.Xml.XmlNodeType.Element:
                    switch (reader.LocalName)
                    {
                        case "info":
                            base.ReadXml(reader);
                            read = false;
                            break;

                        case "question":
                            var question = new Question();
                            question.ReadXml(reader);
                            Questions.Add(question);
                            read = false;
                            break;
                    }

                    break;

                case System.Xml.XmlNodeType.EndElement:
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
    public override void WriteXml(System.Xml.XmlWriter writer)
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
