using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client.Common;

namespace Linq2GraphQL.Tests.Batch;

public class BatchEnvelopeTests
{
    [Fact]
    public void EachSlot_ReadsItsOwnField_OtherFieldsAreSkipped()
    {
        var first = new Slot<string>();
        var second = new Slot<List<int>>();

        var envelope = Read("""{"data":{"b0":"x","other":{"deep":[1,2]},"b1":[1,2,3]}}""",
            ("b0", first), ("b1", second));

        Assert.Equal("x", first.Value);
        Assert.Equal([1, 2, 3], second.Value);
        Assert.True(envelope.IsEnvelope);
        Assert.True(envelope.HasData);
        Assert.Empty(envelope.Errors);
    }

    [Fact]
    public void NullField_IsHandedToItsSlot()
    {
        var slot = new Slot<string> { Value = "unset" };

        Read("""{"data":{"b0":null}}""", ("b0", slot));

        Assert.True(slot.WasRead);
        Assert.Null(slot.Value);
    }

    [Fact]
    public void MissingField_LeavesItsSlotUnread()
    {
        var slot = new Slot<string>();

        Read("""{"data":{"b1":"x"}}""", ("b0", slot));

        Assert.False(slot.WasRead);
    }

    [Fact]
    public void ErrorsBeforeData_AreRead()
    {
        var slot = new Slot<string>();

        var envelope = Read("""{"errors":[{"message":"Bad","path":["b0"]}],"data":{"b0":"x"}}""", ("b0", slot));

        Assert.Equal("x", slot.Value);
        Assert.Equal("Bad", Assert.Single(envelope.Errors).Message);
    }

    [Theory]
    [InlineData("""{"data":null,"errors":[{"message":"Bad","path":["b0"]}]}""", true)]
    [InlineData("""{"errors":[{"message":"Rejected"}]}""", true)]
    [InlineData("""{"extensions":{"cost":1}}""", false)]
    [InlineData("", false)]
    public void WithoutData_HasDataIsFalse(string json, bool isEnvelope)
    {
        var envelope = Read(json, ("b0", new Slot<string>()));

        Assert.False(envelope.HasData);
        Assert.Equal(isEnvelope, envelope.IsEnvelope);
    }

    [Fact]
    public void DataThatIsNotAnObject_Throws()
    {
        Assert.Throws<JsonException>(() => Read("""{"data":[1]}""", ("b0", new Slot<string>())));
    }

    [Theory]
    [InlineData("""{"data":{"b0":{"name":"Ada","age":36}}}""")]
    [InlineData("""{"data":{"b0":null},"errors":[{"message":"Bad","path":["b0"]}],"extensions":{"x":1}}""")]
    [InlineData("""{"data":{"other":1,"b0":{"name":"Ada"}}}""")]
    [InlineData("""{"data":null}""")]
    public void OneSlot_ReadsWhatTheSingleQueryReaderReads(string json)
    {
        var slot = new Slot<Person>();

        var envelope = Read(json, ("b0", slot));
        var single = GraphEnvelopeReader.Read<Person>(Encoding.UTF8.GetBytes(json), "b0", ClientJson.Default);

        Assert.Equal(JsonSerializer.Serialize(single.Data), JsonSerializer.Serialize(slot.Value));
        Assert.Equal(single.Errors?.Count ?? 0, envelope.Errors.Count);
        Assert.Equal(single.Extensions?.Count, envelope.Extensions?.Count);
    }

    private static BatchEnvelope Read(string json, params (string Alias, IGraphBatchSlot Slot)[] slots)
    {
        return GraphEnvelopeReader.ReadBatch(Encoding.UTF8.GetBytes(json),
            slots.ToDictionary(s => s.Alias, s => s.Slot), ClientJson.Default);
    }

    public sealed class Person
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("age")] public int Age { get; set; }
    }

    private sealed class Slot<T> : IGraphBatchSlot
    {
        public T? Value { get; set; }
        public bool WasRead { get; private set; }

        public void ReadData(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            WasRead = true;
            Value = reader.TokenType == JsonTokenType.Null ? default : JsonSerializer.Deserialize<T>(ref reader, options);
        }
    }
}
