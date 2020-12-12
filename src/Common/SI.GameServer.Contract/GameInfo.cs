using SICore;
using SIEngine;
using System;

namespace SI.GameServer.Contract
{
    /// <summary>
    /// Информация о запущенной игре на сервере
    /// </summary>
    public sealed class GameInfo : SimpleGameInfo
    {
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
        public GameStages Stage { get; set; }
        /// <summary>
        /// Текущая стадия игры
        /// </summary>
        public string StageName { get; set; }
        /// <summary>
        /// Правила игры
        /// </summary>
        public GameRules Rules { get; set; }
        public ConnectionPersonData[] Persons { get; set; }
        public bool Started { get; set; }
        public GameModes Mode { get; set; }
        /// <summary>
        /// Язык игры
        /// </summary>
        public string Language { get; set; }
    }
}
