using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SIData
{
    [DataContract]
    public class GameSettingsCore<T>: IGameSettingsCore<T>
        where T: AppSettingsCore, new()
    {
        /// <summary>
        /// Имя живого игрока
        /// </summary>
        [DataMember]
        public string HumanPlayerName { get; set; }

        /// <summary>
        /// Случайные спецвопросы
        /// </summary>
        [XmlAttribute]
        [DefaultValue(false)]
        [DataMember]
        public bool RandomSpecials { get; set; }

        /// <summary>
        /// Имя сетевой игры
        /// </summary>
        [XmlAttribute]
        [DefaultValue("")]
        [DataMember]
        public string NetworkGameName { get; set; }

        /// <summary>
        /// Пароль сетевой игры
        /// </summary>
        [XmlAttribute]
        [DefaultValue("")]
        [DataMember]
        public string NetworkGamePassword { get; set; }

        /// <summary>
        /// Разрешить допуск зрителей в сетевую игру
        /// </summary>
        [XmlAttribute]
        [DefaultValue(false)]
        [DataMember]
        public bool AllowViewers { get; set; }

        /// <summary>
        /// Ведущий игры
        /// </summary>
        [XmlIgnore]
        [DataMember]
        public Account Showman { get; set; }

        /// <summary>
        /// Игроки
        /// </summary>
        [XmlIgnore]
        [DataMember]
        public Account[] Players { get; set; }
        /// <summary>
        /// Зрители
        /// </summary>
        [XmlIgnore]
        [DataMember]
        public Account[] Viewers { get; set; } = Array.Empty<Account>();

        /// <summary>
        /// Настройки, которые могут быть отредактированы пользователем, а также возвращены в состояние по умолчанию
        /// </summary>
        [DataMember]
        public T AppSettings { get; set; } = new T();

		/// <summary>
		/// Является ли игра автоматической
		/// </summary>
		[DefaultValue(false)]
        [DataMember]
        public bool IsAutomatic { get; set; }
	}
}
