using SIPackages.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIPackages
{
    /// <summary>
    /// Represents a package theme.
    /// </summary>
    public sealed class Theme : InfoOwner
    {
        /// <summary>
        /// Theme questions.
        /// </summary>
        public List<Question> Questions { get; } = new List<Question>();

        /// <inheritdoc />
        public override string ToString() => $"{Resources.Theme}: {Name}";

        /// <summary>
        /// Creates a new question in the theme.
        /// </summary>
        /// <param name="price">Question price.</param>
        /// <param name="isFinal">Does the question belong to the final round.</param>
        public Question CreateQuestion(int price = -1, bool isFinal = false)
        {
            int qPrice = DetectQuestionPrice(price, isFinal);

            var quest = new Question
            {
                Price = qPrice
            };

            quest.Scenario.Add(new Atom());
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

            var questionCount = Questions.Count;

            if (questionCount > 1)
            {
                var stepValue = Questions[1].Price - Questions[0].Price;
                return Math.Max(0, Questions[questionCount - 1].Price + stepValue);
            }

            if (questionCount > 0)
            {
                return Questions[0].Price * 2;
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
            _name = reader.GetAttribute("name");

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
            writer.WriteAttributeString("name", _name);
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
                _name = _name
            };

            FillInfo(theme);

            foreach (var quest in Questions)
            {
                theme.Questions.Add(quest.Clone());
            }

            return theme;
        }
    }
}
