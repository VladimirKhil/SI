using System.Collections.Generic;

namespace SIUI.Model
{
    /// <summary>
    /// Данные табло
    /// </summary>
    public sealed class TableInfo
    {
        /// <summary>
        /// Темы игры
        /// </summary>
        public List<string> GameThemes { get; } = new List<string>();

        /// <summary>
        /// Стоимости вопросов в раунде
        /// </summary>
        public List<ThemeInfo> RoundInfo { get; } = new List<ThemeInfo>();

        /// <summary>
        /// Флаг паузы в игре
        /// </summary>
        public bool Pause { get; set; }
    }
}
