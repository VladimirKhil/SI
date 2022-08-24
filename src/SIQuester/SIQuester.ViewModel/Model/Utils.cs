using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SIQuester
{
    /// <summary>
    /// Коллекция вспомогательных методов
    /// </summary>
    internal static class Utils
    {
        private static readonly HttpClient HttpClient = new() { DefaultRequestVersion = HttpVersion.Version20 };

        /// <summary>
        /// Получить XML из Базы
        /// </summary>
        /// <param name="name">Имя турнира в Базе</param>
        /// <returns>Скачанный XML</returns>
        internal static async Task<XmlDocument> GetXmlAsync(string name, CancellationToken cancellationToken)
        {
            using var response = await HttpClient.GetAsync($"http://db.chgk.info/tour/{name}/xml", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync(cancellationToken));
            }

            var xmlDocument = new XmlDocument();

            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            {
                xmlDocument.Load(stream);
            }

            return xmlDocument;
        }

        /// <summary>
        /// Совпадает ли число открывающих и закрывающих скобок в тексте
        /// </summary>
        /// <param name="text">Входной текст</param>
        /// <returns>Совпадает ли число открывающих и закрывающих скобок в тексте</returns>
        internal static bool ValidateTextBrackets(this string text)
        {
            if (text == null)
            {
                return true;
            }

            var total = 0;
            for (var i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '(':
                        total++;
                        break;

                    case ')':
                        if (total > 0)
                        {
                            total--;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                }
            }

            return total == 0;
        }
    }
}
