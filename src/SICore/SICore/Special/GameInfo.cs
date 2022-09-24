using SIData;
using System;

namespace SICore
{
    /// <summary>
    /// Информация о запущенной игре на сервере
    /// </summary>
    public sealed class GameInfo
    {
        public int GameID { get; set; }

        public string GameName { get; set; }

        public string Owner { get; set; }

        public string PackageName { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Дата старта
        /// </summary>
        public DateTime RealStartTime { get; set; }

        /// <summary>
        /// Текущая стадия игры
        /// </summary>
        public string Stage { get; set; }

        /// <summary>
        /// Правила игры
        /// </summary>
        public string Rules { get; set; }

        public bool PasswordRequired { get; set; }

        public ConnectionPersonData[] Persons { get; set; }

        public bool Started { get; set; }

        public SIData.GameModes Mode { get; set; }
    }
}
