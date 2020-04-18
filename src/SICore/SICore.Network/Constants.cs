using System.Text.RegularExpressions;

namespace SICore.Network
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
        /// Имя игры как отдельного клиента
        /// </summary>
        public const string GameName = "@";
        /// <summary>
        /// Мильтикаст-адрес
        /// </summary>
        public const string Everybody = "*";

        /// <summary>
        /// Частичный текст вопроса
        /// </summary>
        public const string PartialText = "partial";

        /// <summary>
        /// Максимальное количество игроков в игре
        /// </summary>
        public const int MaxPlayers = 9;

        public static readonly Regex AddressRegex = new Regex(@"^(?<host>(\d{1,3}\.){3}\d{1,3})\:(?<port>\d+)$");
    }
}
