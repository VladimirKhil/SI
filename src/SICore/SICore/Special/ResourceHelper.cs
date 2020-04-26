using System;
using System.Collections.Generic;
using System.Text;

namespace SICore.Special
{
    public static class ResourceHelper
    {
        /// <summary>
        /// Получить случайную строку ресурса
        /// </summary>
        /// <param name="resource">Строки ресурса, разделённые точкой с запятой</param>
        /// <returns>Одна из строк ресурса (случайная)</returns>
        public static string GetString(string resource)
        {
            var resources = resource.Split(';');
            var index = Data.Rand.Next(resources.Length);

            return resources[index];
        }

        public static string GetSexString(string resource, bool isMale)
        {
            var resources = resource.Split(';');
            if (resources.Length == 1)
                return resources[0];

            return resources[isMale ? 0 : 1];
        }
    }
}
