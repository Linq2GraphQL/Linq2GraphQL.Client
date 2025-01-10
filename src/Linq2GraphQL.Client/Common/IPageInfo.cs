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
        [GraphQLMember("hasNextPage")]
        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }

        [GraphQLMember("hasPreviousPage")]
        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }

        [GraphQLMember("startCursor")]
        [JsonPropertyName("startCursor")]
        public string StartCursor { get; set; }

        [GraphQLMember("endCursor")]
        [JsonPropertyName("endCursor")]
        public string EndCursor { get; set; }

    }
}
