using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Linq2GraphQL.Compat;

/// <summary>
/// Runs every scenario and records, as JSON text, the requests it sent and what it returned or threw. Two runs with
/// the same behavior give the same text.
/// </summary>
internal static class CompatRunner
{
    private static readonly TimeSpan ScenarioTimeout = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions RecordOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new UndefinedJsonElementConverter() }
    };

    public static async Task<SortedDictionary<string, string>> RunAllAsync()
    {
        await using var server = new WebApplicationFactory<Program>();
        await using var nullableServer = new WebApplicationFactory<ProgramNullable>();

        var records = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var scenario in CompatScenarios.All)
        {
            records.Add(scenario.Name, await RunAsync(scenario, new CompatContext(server, nullableServer)));
        }

        return records;
    }

    /// <summary>The value <paramref name="action"/> returns, or a description of what it threw.</summary>
    public static async Task<object> AttemptAsync(Func<Task<object>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception exception)
        {
            return new JsonObject { ["threw"] = Describe(exception) };
        }
    }

    private static async Task<string> RunAsync(CompatScenario scenario, CompatContext context)
    {
        var record = new JsonObject();
        try
        {
            record["result"] = Describe(await scenario.Run(context).WaitAsync(ScenarioTimeout));
        }
        catch (Exception exception)
        {
            record["threw"] = Describe(exception);
        }

        record["requests"] = context.Requests;
        return record.ToJsonString(RecordOptions);
    }

    private static JsonNode Describe(object value)
    {
        try
        {
            return value switch
            {
                null => null,
                JsonNode node => node.DeepClone(),
                _ => JsonSerializer.SerializeToNode(value, value.GetType(), RecordOptions)
            };
        }
        catch (Exception exception)
        {
            return new JsonObject { ["serializationFailed"] = Describe(exception) };
        }
    }

    private static JsonObject Describe(Exception exception)
    {
        var record = new JsonObject
        {
            ["type"] = exception.GetType().FullName,
            ["message"] = exception.Message
        };

        switch (exception)
        {
            case GraphQueryExecutionException execution:
                record["errors"] = Describe(execution.Errors);
                record["query"] = execution.GraphQLQuery;
                record["variables"] = Describe(execution.GraphQLVariables);
                break;
            case GraphQueryRequestException request:
                record["query"] = request.GraphQLQuery;
                record["variables"] = Describe(request.GraphQLVariables);
                break;
            case GraphQueryTranslationException translation:
                record["memberName"] = translation.MemberName;
                break;
        }

        if (exception is AggregateException aggregate)
        {
            record["inner"] = new JsonArray(aggregate.InnerExceptions.Select(inner => (JsonNode)Describe(inner))
                .ToArray());
        }
        else if (exception.InnerException != null)
        {
            record["inner"] = Describe(exception.InnerException);
        }

        return record;
    }

    /// <summary>GraphClient.ExecuteAsync returns an undefined JsonElement when there is no data.</summary>
    private sealed class UndefinedJsonElementConverter : JsonConverter<JsonElement>
    {
        public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options)
        {
            if (value.ValueKind == JsonValueKind.Undefined)
            {
                writer.WriteStringValue("(undefined)");
                return;
            }

            value.WriteTo(writer);
        }
    }
}
