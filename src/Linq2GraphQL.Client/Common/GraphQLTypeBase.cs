using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Linq2GraphQL.Client.Common
{
    public abstract class GraphQLTypeBase
    {

        public T GetMethodValue<T>(string methodName, params object[] arguments)
        {
            var id = Utilities.GetArgumentsId(arguments);

            __AdditionalProperties.TryGetValue(methodName + id, out var value);
            return value.Deserialize<T>();
        }

        public T GetFirstMethodValue<T>(string methodName)
        {
            var keyValue = __AdditionalProperties.FirstOrDefault(e => e.Key.StartsWith(methodName));
            if (keyValue.Key == null)
            {
                return default;
            }

            return keyValue.Value.Deserialize<T>();
        }



        [JsonExtensionData]
        public Dictionary<string, JsonElement> __AdditionalProperties { get; set; } = new();
    }
}
