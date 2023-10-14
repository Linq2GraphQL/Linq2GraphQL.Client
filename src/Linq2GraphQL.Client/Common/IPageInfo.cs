using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Linq2GraphQL.Client.Common
{
    public class PageInfo
    {
        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }
        [JsonPropertyName("startCursor")]
        public string StartCursor { get; set; }
        [JsonPropertyName("endCursor")]
        public string EndCursor { get; set; }

    }
}
