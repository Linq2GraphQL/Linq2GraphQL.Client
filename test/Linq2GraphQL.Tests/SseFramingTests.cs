using System.Net.ServerSentEvents;
using System.Text;
using Linq2GraphQL.Client.Subscriptions;

namespace Linq2GraphQL.Tests;

/// <summary>
///     Framing tests for the SSE wire format as consumed by SSEClient. The parsing is
///     done by System.Net.ServerSentEvents.SseParser (WHATWG compliant); these tests
///     pin the behaviors SSEClient relies on, including the edge cases the previous
///     hand-rolled parser covered.
/// </summary>
public class SseFramingTests
{
    [Fact]
    public async Task SingleDataLine_CompletesOnBlankLine_AsNextEvent()
    {
        var items = await ParseAsync("data: {\"a\":1}\n\n");

        var item = Assert.Single(items);
        // SseParser reports the WHATWG default event type for frames without `event:`.
        Assert.Equal("message", item.EventType);
        Assert.Equal("{\"a\":1}", Encoding.UTF8.GetString(item.Data));
    }

    [Fact]
    public async Task MultipleDataLines_AreJoinedWithNewline()
    {
        var items = await ParseAsync("data: line1\ndata: line2\n\n");

        var item = Assert.Single(items);
        Assert.Equal("line1\nline2", Encoding.UTF8.GetString(item.Data));
    }

    [Fact]
    public async Task EventTypeField_IsPreserved()
    {
        var items = await ParseAsync("event: error\ndata: boom\n\n");

        var item = Assert.Single(items);
        Assert.Equal("error", item.EventType);
        Assert.Equal("boom", Encoding.UTF8.GetString(item.Data));
    }

    [Fact]
    public async Task CommentLines_AreIgnored()
    {
        var items = await ParseAsync(": keepalive\ndata: x\n\n");

        var item = Assert.Single(items);
        Assert.Equal("x", Encoding.UTF8.GetString(item.Data));
    }

    [Fact]
    public async Task CommentOnlyStream_YieldsNothing_KeepsConnectionNeutral()
    {
        // HotChocolate sends ": ka" style comments; they must not produce payloads.
        var items = await ParseAsync(": ping\n: ping\n");

        Assert.Empty(items);
    }

    [Fact]
    public async Task SingleLeadingSpace_IsStrippedFromValue()
    {
        var items = await ParseAsync("data:  spaced\n\n");

        var item = Assert.Single(items);
        Assert.Equal(" spaced", Encoding.UTF8.GetString(item.Data));
    }

    [Fact]
    public async Task FieldWithoutColon_YieldsEmptyValue()
    {
        var items = await ParseAsync("data\n\n");

        var item = Assert.Single(items);
        Assert.Equal("message", item.EventType);
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(item.Data));
    }

    [Fact]
    public async Task IncompleteTrailingEvent_DiscardedAtEof()
    {
        // Data without a terminating blank line is an incomplete frame.
        var items = await ParseAsync("data: truncated");

        Assert.Empty(items);
    }

    [Fact]
    public async Task MultipleEvents_AreAllYielded()
    {
        var items = await ParseAsync("data: one\n\ndata: two\n\n");

        Assert.Equal(2, items.Count);
        Assert.Equal("one", Encoding.UTF8.GetString(items[0].Data));
        Assert.Equal("two", Encoding.UTF8.GetString(items[1].Data));
    }

    [Fact]
    public async Task CompleteEvent_WithEmptyDataField_IsDispatched()
    {
        // GraphQL-over-SSE wire format: `event: complete` + `data:` (empty value).
        var items = await ParseAsync("event: complete\ndata:\n\n");

        var item = Assert.Single(items);
        Assert.Equal("complete", item.EventType);
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(item.Data));
    }

    [Fact]
    public async Task KeepAliveEventType_OnlyFrame_NotDispatched()
    {
        // WHATWG: a frame without a data buffer is never dispatched. HotChocolate's
        // `event: ka\n\n` keep-alives therefore yield nothing here — SSEClient's
        // read loop just keeps waiting for the next real frame.
        var items = await ParseAsync("event: ka\n\n");

        Assert.Empty(items);
    }

    private static async Task<List<SseItem<byte[]>>> ParseAsync(string sse,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sse));
        var parser = SseParser.Create(stream, static (_, data) => data.ToArray());

        var items = new List<SseItem<byte[]>>();
        await foreach (var item in parser.EnumerateAsync(cancellationToken))
        {
            items.Add(item);
        }

        return items;
    }
}
