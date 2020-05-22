using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Services.SI
{
    public sealed class SIStorageService
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer();
        private static readonly HttpClient Client = new HttpClient();

        private readonly string _serverUri;

        static SIStorageService()
        {
            Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");
        }

        public SIStorageService(string serverUri = "https://vladimirkhil.com/api/si")
        {
            _serverUri = serverUri;
        }

        public Task<Package[]> GetAllPackagesAsync(CancellationToken cancellationToken = default) =>
            GetAsync<Package[]>("Packages", cancellationToken);

        public Task<PackageCategory[]> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            GetAsync<PackageCategory[]>("Categories", cancellationToken);

        public Task<Package[]> GetPackagesByCategoryAndRestrictionAsync(int categoryID, string restriction, CancellationToken cancellationToken = default) =>
            GetAsync<Package[]>($"Packages?categoryID={categoryID}&restriction={Uri.EscapeDataString(restriction)}", cancellationToken);

        public Task<Uri> GetPackageByIDAsync(int packageID, CancellationToken cancellationToken = default) =>
            GetAsync<Uri>($"Package?packageID={packageID}", cancellationToken);

        public Task<Uri> GetPackageByGuidAsync(string packageGuid, CancellationToken cancellationToken = default) =>
            GetAsync<Uri>($"PackageByGuid?packageGuid={packageGuid}", cancellationToken);

        public Task<string[]> GetPackagesByTagAsync(int? tagId = null, CancellationToken cancellationToken = default)
        {
            var queryString = new StringBuilder();

            if (tagId.HasValue)
            {
                queryString.Append("tagId=").Append(tagId.Value);
            }

            var packageFilter = queryString.Length > 0 ? $"?{queryString}" : "";
            return GetAsync<string[]>($"PackagesByTag{packageFilter}", cancellationToken);
        }

        public Task<NewServerInfo[]> GetGameServersUrisAsync(CancellationToken cancellationToken = default) =>
            GetAsync<NewServerInfo[]>("GetGameServersUrisNew", cancellationToken);

        public Task<NamedObject[]> GetAuthorsAsync(CancellationToken cancellationToken = default) =>
            GetAsync<NamedObject[]>("Authors", cancellationToken);

        public Task<NamedObject[]> GetPublishersAsync(CancellationToken cancellationToken = default) =>
            GetAsync<NamedObject[]>("Publishers", cancellationToken);

        public Task<NamedObject[]> GetTagsAsync(CancellationToken cancellationToken = default) =>
            GetAsync<NamedObject[]>("Tags", cancellationToken);

        public Task<PackageInfo[]> GetPackagesAsync(int? tagId = null, int difficultyRelation = 0, int difficulty = 1,
            int? publisherId = null, int? authorId = null,
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

                queryString.Append("restriction=").Append(Uri.EscapeDataString(restriction));
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

            return GetAsync<PackageInfo[]>($"FilteredPackages{(queryString.Length > 0 ? $"?{queryString}" : "")}");
        }

        private async Task<T> GetAsync<T>(string request, CancellationToken cancellationToken = default)
        {
            using (var responseMessage = await Client.GetAsync($"{_serverUri}/{request}", cancellationToken))
            {
                if (!responseMessage.IsSuccessStatusCode)
                {
                    throw new Exception($"GetAsync error: {await responseMessage.Content.ReadAsStringAsync()}");
                }

                using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(responseStream))
                {
                    return (T)Serializer.Deserialize(reader, typeof(T));
                }
            }
        }
    }
}
