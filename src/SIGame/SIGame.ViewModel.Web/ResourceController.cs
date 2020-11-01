using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace SIGame.ViewModel.Web
{
    /// <summary>
    /// Контроллер мультимедиа-контента
    /// </summary>
    public sealed class ResourceController : ApiController
    {
        /// <summary>
        /// Получить файл
        /// </summary>
        /// <param name="file">Название файла</param>
        /// <returns>Полученный файл</returns>
        public HttpResponseMessage Get(string file)
        {
            var manager = WebManager.Current;
            if (manager == null)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            }

            var streamInfo = manager.GetFile(file);

            if (streamInfo == null)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            }

            var rangeHeader = Request.Headers.Range;
            var stream = streamInfo.Stream;
            var mediaType = "application/octet-stream";
            var bufferSize = 262144;

            HttpResponseMessage response;
            if (rangeHeader != null && stream.CanSeek)
            {
                try
                {
                    response = Request.CreateResponse(System.Net.HttpStatusCode.PartialContent);
                    response.Content = new ByteRangeStreamContent(stream, rangeHeader, mediaType, bufferSize);
                }
                catch (InvalidByteRangeException)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                }
            }
            else
            {
                response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StreamContent(streamInfo.Stream, bufferSize)
                };

                response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            }

            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            response.Content.Headers.ContentLength = streamInfo.Length;

            response.Headers.AcceptRanges.Add("bytes");

            return response;
        }
    }
}
