using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Services.SI
{
    public sealed class SIStorageService
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer();
        private static readonly HttpClient Client = new HttpClient();

        private readonly string _address;

        static SIStorageService()
        {
            Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");
        }
        
        public SIStorageService(string address = "http://vladimirkhil.com/api/si")
        {
            _address = address;
        }

        public async Task<Package[]> GetAllPackagesAsync()
        {
            return await Call<Package[]>("Packages");
        }

        public async Task<PackageCategory[]> GetCategoriesAsync()
        {
            return await Call<PackageCategory[]>("Categories");
        }

        public async Task<Package[]> GetPackagesByCategoryAndRestrictionAsync(int categoryID, string restriction)
        {
            return await Call<Package[]>("Packages?categoryID=" + categoryID + "&restriction=" + Uri.EscapeDataString(restriction));
        }

        public async Task<Uri> GetPackageByIDAsync(int packageID)
        {
            return await Call<Uri>("Package?packageID=" + packageID);
        }

        public async Task<Uri> GetPackageByGuidAsync(string packageGuid)
        {
            return await Call<Uri>("PackageByGuid?packageGuid=" + packageGuid);
        }

        public async Task<string[]> GetPackagesByTagAsync(int? tagId = null)
        {
            var queryString = new StringBuilder();

            if (tagId.HasValue)
            {
                queryString.Append("tagId=").Append(tagId.Value);
            }

            return await Call<string[]>("PackagesByTag" + (queryString.Length > 0 ? "?" + queryString.ToString() : ""));
        }

        public async Task<NewServerInfo[]> GetGameServersUrisAsync()
        {
            return await Call<NewServerInfo[]>("GetGameServersUrisNew");
        }

        public async Task<NamedObject[]> GetAuthorsAsync()
        {
            return await Call<NamedObject[]>("Authors");
        }

        public async Task<NamedObject[]> GetPublishersAsync()
        {
            return await Call<NamedObject[]>("Publishers");
        }

        public async Task<NamedObject[]> GetTagsAsync()
        {
            return await Call<NamedObject[]>("Tags");
        }

        public async Task<PackageInfo[]> GetPackagesAsync(int? tagId = null, int difficultyRelation = 0, int difficulty = 1, int? publisherId = null, int? authorId = null,
            string restriction = null, PackageSortMode sortMode = PackageSortMode.Name, bool sortAscending = true)
        {
            var queryString = new StringBuilder();

            if (tagId.HasValue)
            {
                if (queryString.Length > 0)
                    queryString.Append('&');

                queryString.Append("tagId=").Append(tagId.Value);
            }

            if (difficultyRelation > 0)
            {
                if (queryString.Length > 0)
                    queryString.Append('&');

                queryString.Append("difficultyRelation=").Append(difficultyRelation);
            }

            if (difficulty > 1)
            {
                if (queryString.Length > 0)
                    queryString.Append('&');

                queryString.Append("difficulty=").Append(difficulty);
            }

            if (publisherId.HasValue)
            {
                if (queryString.Length > 0)
                    queryString.Append('&');

                queryString.Append("publisherId=").Append(publisherId.Value);
            }

            if (authorId.HasValue)
            {
                if (queryString.Length > 0)
                    queryString.Append('&');

                queryString.Append("authorId=").Append(authorId.Value);
            }

            if (restriction != null)
            {
                if (queryString.Length > 0)
                    queryString.Append('&');

                queryString.Append("restriction=").Append(restriction);
            }

            if (sortMode != PackageSortMode.Name)
            {
                if (queryString.Length > 0)
                    queryString.Append('&');

                queryString.Append("sortMode=").Append((int)sortMode);
            }

            if (!sortAscending)
            {
                if (queryString.Length > 0)
                    queryString.Append('&');

                queryString.Append("sortAscending=false");
            }

            return await Call<PackageInfo[]>("FilteredPackages" + (queryString.Length > 0 ? "?" + queryString.ToString() : ""));
        }

        private async Task<T> Call<T>(string request)
        {
            using (var stream = await Client.GetStreamAsync(_address + "/" + request))
            {
                using (var reader = new StreamReader(stream))
                {
                    return (T)Serializer.Deserialize(reader, typeof(T));
                }
            }
        }
    }
}
