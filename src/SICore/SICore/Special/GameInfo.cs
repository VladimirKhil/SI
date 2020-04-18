using SIEngine;
using System;
using System.Runtime.Serialization;

namespace SICore
{
    /// <summary>
    /// Информация о запущенной игре на сервере
    /// </summary>
    [DataContract(Name = "GameData")]
    public sealed class GameInfo
    {
        [DataMember]
        public int GameID { get; set; }
        [DataMember]
        public string GameName { get; set; }
        [DataMember]
        public string Owner { get; set; }
        [DataMember]
        public string PackageName { get; set; }
        /// <summary>
        /// Дата создания
        /// </summary>
        [DataMember]
        public DateTime StartTime { get; set; }
        /// <summary>
        /// Дата старта
        /// </summary>
        [DataMember]
        public DateTime RealStartTime { get; set; }
        /// <summary>
        /// Текущая стадия игры
        /// </summary>
        [DataMember]
        public string Stage { get; set; }
        /// <summary>
        /// Правила игры
        /// </summary>
        [DataMember]
        public string Rules { get; set; }
        [DataMember]
        public bool PasswordRequired { get; set; }
        [DataMember]
        public ConnectionPersonData[] Persons { get; set; }
		[DataMember]
		public bool Started { get; set; }
		[DataMember]
		public GameModes Mode { get; set; }
	}
}
