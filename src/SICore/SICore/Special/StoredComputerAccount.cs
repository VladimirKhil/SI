using Newtonsoft.Json;
using System.Collections.Generic;

namespace SICore
{
    public sealed class StoredComputerAccount: ComputerAccount
    {
        private const string UnsetName = "<Unset name>";

        [JsonProperty]
        public new bool IsMale { get; set; }

        [JsonProperty]
        public IDictionary<string, string> Names { get; set; }

        public string GetLocalizedName(string cultureCode)
        {
            if (Names.TryGetValue(cultureCode, out var name))
            {
                return name;
            }

            return UnsetName;
        }
    }
}
