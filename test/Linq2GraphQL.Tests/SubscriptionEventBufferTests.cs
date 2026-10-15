using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Subscriptions;

namespace Linq2GraphQL.Tests;

public class SubscriptionEventBufferTests
{
    [Fact]
    public void FaultPolicy_Overflow_FaultsAndRefusesFurtherEnqueues()
    {
        var buffer = new SubscriptionEventBuffer<int>(2, SubscriptionOverflowPolicy.Fault);

        Assert.True(buffer.TryEnqueue(1, out _));
        Assert.True(buffer.TryEnqueue(2, out _));

        Assert.False(buffer.TryEnqueue(3, out var fault));
        Assert.IsType<InvalidOperationException>(fault);
        Assert.Contains("2", fault.Message);

        // A faulted buffer stays faulted and keeps the original events.
        Assert.False(buffer.TryEnqueue(4, out _));
        Assert.Equal(2, buffer.Count);
    }

    [Fact]
    public void DropOldestPolicy_Overflow_NewestEventsWin()
    {
        var buffer = new SubscriptionEventBuffer<int>(2, SubscriptionOverflowPolicy.DropOldest);

        buffer.TryEnqueue(1, out _);
        buffer.TryEnqueue(2, out _);
        Assert.True(buffer.TryEnqueue(3, out _));

        Assert.Equal(2, buffer.Count);
        Assert.True(buffer.TryDequeue(out var first));
        Assert.Equal(2, first);
        Assert.True(buffer.TryDequeue(out var second));
        Assert.Equal(3, second);
        Assert.False(buffer.TryDequeue(out _));
    }

    [Fact]
    public void KeepLatestPolicy_Overflow_KeepsOnlyMostRecent()
    {
        var buffer = new SubscriptionEventBuffer<int>(3, SubscriptionOverflowPolicy.KeepLatest);

        buffer.TryEnqueue(1, out _);
        buffer.TryEnqueue(2, out _);
        buffer.TryEnqueue(3, out _); // now full
        Assert.True(buffer.TryEnqueue(4, out _)); // overflow → keep latest only

        Assert.Equal(1, buffer.Count);
        Assert.True(buffer.TryDequeue(out var only));
        Assert.Equal(4, only);
    }

    [Fact]
    public void KeepLatestPolicy_MaxOne_AlwaysMostRecent()
    {
        var buffer = new SubscriptionEventBuffer<int>(1, SubscriptionOverflowPolicy.KeepLatest);

        buffer.TryEnqueue(1, out _);
        buffer.TryEnqueue(2, out _);
        buffer.TryEnqueue(3, out _);

        Assert.True(buffer.TryDequeue(out var only));
        Assert.Equal(3, only);
    }

    [Fact]
    public void NonPositiveLimit_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SubscriptionEventBuffer<int>(0, SubscriptionOverflowPolicy.Fault));
    }

    [Fact]
    public void Dequeue_EmptyBuffer_ReturnsFalse()
    {
        var buffer = new SubscriptionEventBuffer<int>(4, SubscriptionOverflowPolicy.Fault);

        Assert.False(buffer.TryDequeue(out var payload));
        Assert.Equal(0, payload);
    }

    [Fact]
    public void UnderLimit_NoFault()
    {
        var buffer = new SubscriptionEventBuffer<int>(128, SubscriptionOverflowPolicy.Fault);

        for (var i = 0; i < 128; i++)
        {
            Assert.True(buffer.TryEnqueue(i, out var fault));
            Assert.Null(fault);
        }

        Assert.Equal(128, buffer.Count);
    }
}
