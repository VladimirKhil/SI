using SIPackages.Properties;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SIPackages
{
    /// <summary>
    /// Пакет
    /// </summary>
    public sealed class Package : InfoOwner
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private double _version = 4.0;

        /// <summary>
        /// Версия пакета
        /// </summary>
        public double Version { get { return _version; } }

        private string _id = "";

        /// <summary>
        /// Уникальный идентификатор пакета
        /// </summary>
        public string ID
        {
            get { return _id; }
            set
            {
                var oldValue = _id;
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        private string _restriction = "";

        /// <summary>
        /// Ограничение на использование пакета (по возрасту, региону и проч.)
        /// </summary>
        public string Restriction
        {
            get { return _restriction; }
            set
            {
                var oldValue = _restriction;
                if (_restriction != value)
                {
                    _restriction = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        private string _publisher = "";

        /// <summary>
        /// Издатель пакета
        /// </summary>
        public string Publisher
        {
            get { return _publisher; }
            set
            {
                var oldValue = _publisher;
                if (_publisher != value)
                {
                    _publisher = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        private int _difficulty = 5;

        /// <summary>
        /// Сложность пакета
        /// </summary>
        public int Difficulty
        {
            get { return _difficulty; }
            set
            {
                var oldValue = _difficulty;
                if (_difficulty != value)
                {
                    _difficulty = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        private string _logo = "";

        /// <summary>
        /// Адрес логотипа пакета
        /// </summary>
        public string Logo
        {
            get { return _logo; }
            set
            {
                var oldValue = _logo;
                if (_logo != value)
                {
                    _logo = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        private string _date = "";

        /// <summary>
        /// Дата создания пакета (задана произвольным образом - 2005 год, 31.12.2014, осень 2008 и проч.)
        /// </summary>
        public string Date
        {
            get { return _date; }
            set
            {
                var oldValue = _date;
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        private string _language = "";

        /// <summary>
        /// Язык пакета
        /// </summary>
        public string Language
        {
            get { return _language; }
            set
            {
                var oldValue = _language;
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged(oldValue);
                }
            }
        }

        /// <summary>
        /// Тематики пакета
        /// </summary>
        public List<string> Tags { get; } = new List<string>();

        /// <summary>
        /// Раунды пакета
        /// </summary>
        public List<Round> Rounds { get; } = new List<Round>();

        /// <summary>
        /// Строковое представление пакета
        /// </summary>
        /// <returns>Описание пакета</returns>
        public override string ToString() => $"{Resources.Package}: {Resources.Package}";

        /// <summary>
        /// Создание раунда
        /// </summary>
        /// <param name="type">Тип раунда</param>
        /// <param name="name">Имя раунда</param>
        public Round CreateRound(string type, string name)
        {
            var round = new Round
            {
                Name = name ?? ((int)(Rounds.Count + 1)).ToString() + Resources.RoundTrailing,
                Type = type
            };

            Rounds.Add(round);
            return round;
        }

        /// <summary>
        /// Reads data from XML reader.
        /// </summary>
        /// <param name="reader">XML reader.</param>
        public override void ReadXml(System.Xml.XmlReader reader)
        {
            _name = reader.GetAttribute("name");

            var versionString = reader.GetAttribute("version");
            double.TryParse(versionString, out _version);

            if (reader.MoveToAttribute("id"))
                _id = reader.Value;

            if (reader.MoveToAttribute("restriction"))
                _restriction = reader.Value;

            if (reader.MoveToAttribute("date"))
                _date = reader.Value;

            if (reader.MoveToAttribute("publisher"))
                _publisher = reader.Value;

            if (reader.MoveToAttribute("difficulty"))
                int.TryParse(reader.Value, out _difficulty);

            if (reader.MoveToAttribute("logo"))
                _logo = reader.Value;

            if (reader.MoveToAttribute("language"))
                _language = reader.Value;

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
                            case "tag":
                                Tags.Add(reader.ReadElementContentAsString());
                                read = false;
                                break;

                            case "info":
                                base.ReadXml(reader);
                                read = false;
                                break;

                            case "round":
                                var round = new Round();
                                round.ReadXml(reader);
                                Rounds.Add(round);
                                read = false;
                                break;
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
            writer.WriteStartElement("package", "http://vladimirkhil.com/ygpackage3.0.xsd");
            writer.WriteAttributeString("name", _name);
            writer.WriteAttributeString("version", _version.ToString());

            if (!string.IsNullOrEmpty(_id))
                writer.WriteAttributeString("id", _id);

            if (!string.IsNullOrEmpty(_restriction))
                writer.WriteAttributeString("restriction", _restriction);

            if (!string.IsNullOrEmpty(_date))
                writer.WriteAttributeString("date", _date);

            if (!string.IsNullOrEmpty(_publisher))
                writer.WriteAttributeString("publisher", _publisher);

            if (_difficulty > 0)
                writer.WriteAttributeString("difficulty", _difficulty.ToString());

            if (!string.IsNullOrEmpty(_logo))
                writer.WriteAttributeString("logo", _logo);

            if (!string.IsNullOrEmpty(_language))
                writer.WriteAttributeString("language", _language);

            if (Tags.Count > 0)
            {
                writer.WriteStartElement("tags");

                foreach (var item in Tags)
                {
                    writer.WriteElementString("tag", item);
                }

                writer.WriteEndElement();
            }

            base.WriteXml(writer);

            if (Rounds.Any())
            {
                writer.WriteStartElement("rounds");
                foreach (var item in Rounds)
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
        public Package Clone()
        {
            var package = new Package
            {
                _name = _name,
                _date = _date,
                _restriction = _restriction,
                _publisher = _publisher,
                _difficulty = _difficulty,
                _logo = _logo
            };

            package.Tags.AddRange(Tags);

            FillInfo(package);

            foreach (var round in Rounds)
            {
                package.Rounds.Add(round.Clone());
            }

            return package;
        }
    }
}
