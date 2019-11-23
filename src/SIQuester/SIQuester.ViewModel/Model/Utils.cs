using System;
using System.Net;
using System.Xml;
using SIQuester.Model;
using System.Threading.Tasks;

namespace SIQuester
{
    /// <summary>
    /// Коллекция вспомогательных методов
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Получить XML из Базы
        /// </summary>
        /// <param name="name">Имя турнира в Базе</param>
        /// <returns>Скачанный XML</returns>
        internal static async Task<XmlDocument> GetXml(string name)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(String.Format("http://db.chgk.info/tour/{0}/xml", name));
            webRequest.UserAgent = AppSettings.UserAgentHeader;

            var response = await webRequest.GetResponseAsync();

            var xmlDocument = new XmlDocument();
            using (var stream = response.GetResponseStream())
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
        internal static bool GoodBrackets(this string text)
        {
            if (text == null)
                return true;

            int total = 0;
            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '(':
                        total++;
                        break;

                    case ')':
                        if (total > 0)
                            total--;
                        else
                            return false;
                        break;
                }
            }

            return true;
        }
    }
}
