using SIPackages.Core;
using SIPackages.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SIPackages
{
    /// <summary>
    /// Раунд
    /// </summary>
    public sealed class Round : InfoOwner
    {
        public List<Theme> Themes { get; } = new List<Theme>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _type = RoundTypes.Standart;

        /// <summary>
        /// Тип раунда
        /// standart: обычный
        /// final: финальный
        /// </summary>
        public string Type
        {
            get => _type;
            set { var oldValue = _type; if (oldValue != value) { _type = value; OnPropertyChanged(oldValue); } }
        }

        public override string ToString() => $"{Resources.Round}: {Resources.Round}";

        /// <summary>
        /// Создание темы
        /// </summary>
        /// <param name="name">Название темы</param>
        public Theme CreateTheme(string name)
        {
            var theme = new Theme { Name = name ?? "" };
            Themes.Add(theme);
            return theme;
        }

        public override void ReadXml(System.Xml.XmlReader reader)
        {
            Name = reader.GetAttribute("name");
            if (reader.MoveToAttribute("type"))
                _type = reader.Value;

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

                            case "theme":
                                var theme = new Theme();
                                theme.ReadXml(reader);
                                Themes.Add(theme);
                                read = false;
                                break;
                        }

                        break;

                    case System.Xml.XmlNodeType.EndElement:
                        if (reader.LocalName == "round")
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
            writer.WriteStartElement("round");
            writer.WriteAttributeString("name", _name);
            if (_type != RoundTypes.Standart)
                writer.WriteAttributeString("type", _type);
            base.WriteXml(writer);

            if (Themes.Any())
            {
                writer.WriteStartElement("themes");
                foreach (var item in Themes)
                {
                    item.WriteXml(writer);
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public Round Clone()
        {
            var round = new Round()
            {
                _name = _name,
                _type = _type
            };

            FillInfo(round);

            foreach (var theme in Themes)
            {
                round.Themes.Add(theme.Clone());
            }

            return round;
        }
    }
}
