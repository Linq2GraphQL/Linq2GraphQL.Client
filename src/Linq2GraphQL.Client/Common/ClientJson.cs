using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linq2GraphQL.Client.Common;

/// <summary>
///     Shared, frozen JsonSerializerOptions for all GraphClient instances.
///     System.Text.Json caches type metadata per options instance, so a single shared
///     instance avoids rebuilding converters and property metadata on every request.
///     Values are identical to the historical per-client configuration.
/// </summary>
internal static class ClientJson
{
    internal static readonly JsonSerializerOptions Default = CreateDefault();

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { },
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
