﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIPackages.Core;

namespace SIPackages.PlatformSpecific.Net45
{
    internal sealed class FolderSIPackage : ISIPackage
    {
        private string _folder;

        public ISIPackage CopyTo(Stream stream, bool close, out bool isNew)
        {
            throw new NotImplementedException();
        }

        internal static ISIPackage Create(string folder)
        {
            return new FolderSIPackage { _folder = folder };
        }

        internal static ISIPackage Open(string folder)
        {
            return new FolderSIPackage { _folder = folder };
        }

        public void CreateStream(string name, string contentType)
        {
            using (File.Create(Path.Combine(_folder, name))) { }
        }

        public void CreateStream(string category, string name, string contentType)
        {
            Directory.CreateDirectory(Path.Combine(_folder, category));
            using (File.Create(Path.Combine(_folder, category, name))) { }
        }

        public async Task CreateStream(string category, string name, string contentType, Stream stream)
        {
            Directory.CreateDirectory(Path.Combine(_folder, category));
            using (var fs = File.Create(Path.Combine(_folder, category, name)))
            {
                await stream.CopyToAsync(fs);
            }
        }

        public void DeleteStream(string category, string name)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public void Flush()
        {
            
        }

        public string[] GetEntries(string category)
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(this._folder, category));
            if (!directoryInfo.Exists)
                return new string[0];

            return directoryInfo.GetFiles().Select(file => file.Name).ToArray();
        }

        public StreamInfo GetStream(string name, bool read = true)
        {
            var file = new FileInfo(Path.Combine(this._folder, name));
            if (!file.Exists)
                return null;

            return new StreamInfo { Length = file.Length, Stream = read ? file.OpenRead() : file.Open(FileMode.Open) };
        }

        public StreamInfo GetStream(string category, string name, bool read = true)
        {
            if (name.Length > ZipHelper.MaxFileNameLength)
                name = ZipHelper.CalculateHash(name);

            return GetStream(Path.Combine(category, name), read);
        }
    }
}
