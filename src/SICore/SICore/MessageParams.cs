using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICore
{
    public static class MessageParams
    {
        /// <summary>
        /// Адрес расположения единицы сценария
        /// </summary>
        public const string Atom_Uri = "URI";

        /// <summary>
        /// Добавить стол игрока
        /// </summary>
        public const string Config_AddTable = "ADDTABLE";

        /// <summary>
        /// Удалить стол игрока
        /// </summary>
        public const string Config_DeleteTable = "DELETETABLE";

        /// <summary>
        /// Освободить стол игрока/ведущего
        /// </summary>
        public const string Config_Free = "FREE";

        /// <summary>
        /// Посадить за стол игрока/ведущего
        /// </summary>
        public const string Config_Set = "SET";

        /// <summary>
        /// Изменить тип игрока/ведущего
        /// </summary>
        public const string Config_ChangeType = "CHANGETYPE";

        /// <summary>
        /// Обновление игровой конфигурации
        /// </summary>
        public const string Info_Update = "UPDATE";
    }
}
