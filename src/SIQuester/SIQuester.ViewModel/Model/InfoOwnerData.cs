using SIPackages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SIQuester.Model
{
    [Serializable]
    public sealed class InfoOwnerData : IDisposable
    {
        public enum Level { Package, Round, Theme, Question };

        public Level ItemLevel { get; set; }
        public string ItemData { get; set; }
        public List<AuthorInfo> Authors { get; set; }
        public List<SourceInfo> Sources { get; set; }
        public Dictionary<string, StreamProxy> Images { get; set; }
        public Dictionary<string, StreamProxy> Audio { get; set; }
        public Dictionary<string, StreamProxy> Video { get; set; }

        public InfoOwnerData(InfoOwner item)
        {
            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                item.WriteXml(writer);
            }

            ItemData = sb.ToString();
            ItemLevel =
                item is Package ? Level.Package :
                item is Round ? Level.Round :
                item is Theme ? Level.Theme : Level.Question;
        }

        public InfoOwner GetItem()
        {
            InfoOwner item = ItemLevel switch
            {
                Level.Package => new Package(),
                Level.Round => new Round(),
                Level.Theme => new Theme(),
                _ => new Question(),
            };

            using (var sr = new StringReader(ItemData))
            {
                using var reader = XmlReader.Create(sr);
                reader.Read();
                item.ReadXml(reader);
            }

            return item;
        }

        public void Dispose()
        {
            if (Images != null)
            {
                foreach (var item in Images.Values)
                {
                    item.Dispose();
                }
            }

            if (Audio != null)
            {
                foreach (var item in Audio.Values)
                {
                    item.Dispose();
                }
            }

            if (Video != null)
            {
                foreach (var item in Video.Values)
                {
                    item.Dispose();
                }
            }
        }

        /// <summary>
        /// Получить полные данные для объекта, включая присоединённые элементы коллекций
        /// </summary>
        /// <returns></returns>
        public void GetFullData(SIDocument document, InfoOwner owner)
        {
            var length = owner.Info.Authors.Count;
            for (int i = 0; i < length; i++)
            {
                var docAuthor = document.GetLink(owner.Info.Authors, i);
                if (docAuthor != null)
                {
                    if (Authors == null)
                        Authors = new List<AuthorInfo>();

                    if (!Authors.Contains(docAuthor))
                        Authors.Add(docAuthor);
                }
            }

            length = owner.Info.Sources.Count;
            for (int i = 0; i < length; i++)
            {
                var docSource = document.GetLink(owner.Info.Sources, i);
                if (docSource != null)
                {
                    if (Sources == null)
                        Sources = new List<SourceInfo>();

                    if (!Sources.Contains(docSource))
                        Sources.Add(docSource);
                }
            }
        }

        public async Task ApplyData(SIDocument document)
        {
            if (Authors != null)
            {
                foreach (var author in Authors)
                {
                    if (!document.Authors.Any(x => x.Id == author.Id))
                    {
                        document.Authors.Add(author);
                    }
                }
            }

            if (Sources != null)
            {
                foreach (var source in Sources)
                {
                    if (!document.Sources.Any(x => x.Id == source.Id))
                    {
                        document.Sources.Add(source);
                    }
                }
            }

            if (Images != null)
            {
                foreach (var item in Images)
                {
                    if (!document.Images.Contains(item.Key))
                    {
                        await document.Images.AddFile(item.Key, item.Value.Stream);
                    }
                }
            }

            if (Audio != null)
            {
                foreach (var item in Audio)
                {
                    if (!document.Audio.Contains(item.Key))
                    {
                        await document.Audio.AddFile(item.Key, item.Value.Stream);
                    }
                }
            }

            if (Video != null)
            {
                foreach (var item in Video)
                {
                    if (!document.Video.Contains(item.Key))
                    {
                        await document.Video.AddFile(item.Key, item.Value.Stream);
                    }
                }
            }
        }
    }
}
