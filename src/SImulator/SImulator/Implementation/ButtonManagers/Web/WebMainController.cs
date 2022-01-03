#if LEGACY
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace SImulator.Implementation.ButtonManagers.Web
{
    public sealed class WebMainController : ApiController
    {
        /// <summary>
        /// Получить материал
        /// </summary>
        /// <param name="gameID">Идентификатор игры</param>
        /// <param name="file">Название файла</param>
        /// <returns></returns>
        public HttpResponseMessage Get(string file)
        {
            return GetFile("Web", file);
        }

        private static HttpResponseMessage GetFile(string folder, string file)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder, file);
            if (!File.Exists(path))
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);

            var stream = File.OpenRead(path);

            var contentType = "text/html";
            var extension = Path.GetExtension(file);
            if (extension == ".css")
                contentType = "text/css";
            else if (extension == ".js")
                contentType = "text/javascript";

            var result = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            result.Content.Headers.ContentLength = stream.Length;
            return result;
        }

        public HttpResponseMessage GetScript(string file)
        {
            return GetFile("Scripts", file);
        }
    }
}
#endif
