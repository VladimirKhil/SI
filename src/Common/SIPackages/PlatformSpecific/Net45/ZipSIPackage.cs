using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace SIPackages.PlatformSpecific.Net45
{
    internal sealed class ZipSIPackage : ISIPackage
    {
        private readonly Stream _stream;
        private readonly ZipArchive _zipArchive;

        private Dictionary<string, string> _contentTypes = new Dictionary<string, string>();

        private ZipSIPackage(Stream stream, ZipArchive zipArchive)
        {
            _stream = stream;
            _zipArchive = zipArchive;
        }

        public static ZipSIPackage Create(Stream stream, bool leaveOpen = false) =>
            new(stream, new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen));

        public static ZipSIPackage Open(Stream stream, bool read = true)
        {
            var zipPackage = new ZipSIPackage(
                stream,
                new ZipArchive(stream, read ? ZipArchiveMode.Read : ZipArchiveMode.Update, false));

            var entry = zipPackage._zipArchive.GetEntry("[Content_Types].xml");
            if (entry != null)
            {
                using var readStream = entry.Open();
                using var reader = XmlReader.Create(readStream);

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "Default")
                    {
                        var ext = reader["Extension"];
                        var type = reader["ContentType"];

                        zipPackage._contentTypes[ext.ToLower()] = type;
                    }
                }
            }

            return zipPackage;
        }

        public string[] GetEntries(string category)
        {
            if (_zipArchive.Mode == ZipArchiveMode.Create)
            {
                return Array.Empty<string>();
            }

            return _zipArchive.Entries
                .Where(entry => entry.FullName.StartsWith(category))
                .Select(entry => Uri.UnescapeDataString(entry.Name))
                .ToArray();
        }

        public StreamInfo GetStream(string name, bool read = true)
        {
            var entry = _zipArchive.GetEntry(name);
            if (entry == null)
            {
                return null;
            }
            
            var stream = entry.Open();
            if (!read)
            {
                stream.SetLength(0);
            }

            return new StreamInfo { Stream = stream, Length = _zipArchive.Mode == ZipArchiveMode.Read ? entry.Length : 0 };
        }

        public StreamInfo GetStream(string category, string name, bool read = true) =>
            GetStream($"{category}/{Uri.EscapeUriString(name)}", read);

        public void CreateStream(string name, string contentType)
        {
            _zipArchive.CreateEntry(Uri.EscapeUriString(name), CompressionLevel.Optimal);
            AddContentTypeInfo(name, contentType);
        }

        private void AddContentTypeInfo(string name, string contentType)
        {
            var extension = Path.GetExtension(name);
            if (extension.StartsWith("."))
            {
                extension = extension.Substring(1);
            }

            var ext = extension.ToLower();
            if (!_contentTypes.ContainsKey(ext))
            {
                _contentTypes[ext] = contentType;
            }
        }

        public void CreateStream(string category, string name, string contentType)
        {
            _zipArchive.CreateEntry(category + "/" + Uri.EscapeUriString(name), CompressionLevel.Optimal);
            AddContentTypeInfo(name, contentType);
        }

        public async Task CreateStreamAsync(string category, string name, string contentType, Stream stream)
        {
            var entry = _zipArchive.CreateEntry(category + "/" + Uri.EscapeUriString(name), CompressionLevel.Optimal);
            using (var writeStream = entry.Open())
            {
                await stream.CopyToAsync(writeStream);
            }

            AddContentTypeInfo(name, contentType);
        }

        public void DeleteStream(string category, string name)
        {
            _zipArchive.GetEntry(category + "/" + Uri.EscapeUriString(name)).Delete();
        }

        public ISIPackage CopyTo(Stream stream, bool closeCurrent, out bool isNew)
        {
            if (_stream.Length == 0)
            {
                // Это новый пакет, копировать нечего
                isNew = true;
                var package = Create(stream);

                package._contentTypes = _contentTypes;

                return package;
            }
            
            isNew = false;

            _stream.Position = 0; // обязательно нужно
            _stream.CopyTo(stream);
            stream.Position = 0;

            // Переоткрываем
            if (closeCurrent)
            {
                _stream.Dispose(); // what about _zipPackage?
            }

            return Open(stream, false);
        }

        public void Dispose() => _zipArchive.Dispose();

        public void Flush()
        {
            // Для обратной совместимости
            var entry = _zipArchive.GetEntry("[Content_Types].xml");

            if (entry != null)
            {
                entry.Delete();
            }

            entry = _zipArchive.CreateEntry("[Content_Types].xml", CompressionLevel.Optimal);

            using (var writeStream = entry.Open())
            {
                var ns = "http://schemas.openxmlformats.org/package/2006/content-types";
                using var writer = XmlWriter.Create(writeStream);

                writer.WriteStartElement("Types", ns);
                foreach (var item in _contentTypes)
                {
                    writer.WriteStartElement("Default", ns);
                    writer.WriteAttributeString("Extension", item.Key);
                    writer.WriteAttributeString("ContentType", item.Value);
                    writer.WriteEndElement();
                }
            }

            _stream.Flush();
        }
    }
}
