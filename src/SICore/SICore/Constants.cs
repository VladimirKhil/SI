namespace SICore
{
    /// <summary>
    /// Игровые константы (не путать с ресурсами, переводить нельзя)
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Обозначение имени игрового хоста в Uri
        /// </summary>
        public const string GameHost = "<GAMEHOST>";
        public const string ServerHost = "<SERVERHOST>";

        /// <summary>
        /// Свободный (пустой аккаунт)
        /// </summary>
        public const string FreePlace = " ";

        /// <summary>
        /// Частичный текст вопроса
        /// </summary>
        public const string PartialText = "partial";

        /// <summary>
        /// Максимальное количество игроков в игре
        /// </summary>
        public const int MaxPlayers = 12;

        /// <summary>
        /// Интервал запуска автоматической игры
        /// </summary>
        public const int AutomaticGameStartDuration = 300;

        /// <summary>
        /// Ведущий
        /// </summary>
        public const string Showman = "showman";

        /// <summary>
        /// Игрок
        /// </summary>
        public const string Player = "player";

        /// <summary>
        /// Место для подстановки ответа
        /// </summary>
        public const string AnswerPlaceholder = "#";
    }
}
