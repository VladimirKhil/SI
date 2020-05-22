using SIPackages.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIPackages
{
    /// <summary>
    /// Тема
    /// </summary>
    public sealed class Theme : InfoOwner
    {
        public List<Question> Questions { get; } = new List<Question>();

        /// <summary>
        /// Строковое представление темы
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Resources.Theme}: {Name}";

        /// <summary>
        /// Создание нового вопроса в теме
        /// </summary>
        /// <param name="price">Цена вопроса</param>
        public Question CreateQuestion(int price = -1, bool isFinal = false)
        {
            int qPrice;
            if (price == -1)
            {
                var n = Questions.Count;
                if (n > 1)
                {
                    var add = Questions[1].Price - Questions[0].Price;
                    qPrice = Math.Max(0, Questions[n - 1].Price + add);
                }
                else if (n > 0)
                    qPrice = Questions[0].Price * 2;
                else
                    if (isFinal)
                        qPrice = 0;
                    else
                        qPrice = 100;
            }
            else
                qPrice = price;

            var quest = new Question
            {
                Price = qPrice
            };
            var atom = new Atom();
            quest.Scenario.Add(atom);
            quest.Right.Add("");
            Questions.Add(quest);

            return quest;
        }

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

        public Theme Clone()
        {
            var theme = new Theme()
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
