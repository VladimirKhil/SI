using Newtonsoft.Json;
using System.Collections.Generic;

namespace SICore
{
	public sealed class StoredComputerAccount: ComputerAccount
	{
		[JsonProperty]
		public new bool IsMale { get; set; }
		[JsonProperty]
		public IDictionary<string, string> Names { get; set; }
	}
}
