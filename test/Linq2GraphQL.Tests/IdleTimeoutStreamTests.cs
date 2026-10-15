using System.Text;
using Linq2GraphQL.Client.Subscriptions;

namespace Linq2GraphQL.Tests;

public class IdleTimeoutStreamTests
{
    [Fact]
    public async Task NoDataWithinTimeout_FailsWithIOException()
    {
        using var stream = new IdleTimeoutStream(new NeverendingStream(), TimeSpan.FromMilliseconds(100));

        var exception = await Assert.ThrowsAsync<IOException>(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
#pragma warning disable CA2022 // The read is expected to throw (idle timeout); the byte count is irrelevant.
            await stream.ReadAsync(new byte[16], cts.Token);
#pragma warning restore CA2022
        });

        Assert.Contains("idle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DataArrivingWithinWindow_ReadsSucceed()
    {
        var data = Encoding.UTF8.GetBytes("hello");
        using var stream = new IdleTimeoutStream(new MemoryStream(data), TimeSpan.FromMilliseconds(500));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var buffer = new byte[16];
        var total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(total), cts.Token)) > 0)
        {
            total += read;
        }

        Assert.Equal(5, total);
        Assert.Equal("hello", Encoding.UTF8.GetString(buffer, 0, total));
    }

    [Fact]
    public async Task SlowButRegularData_DoesNotTimeOut()
    {
        // One byte every 50ms for ~1s with a 750ms idle timeout: the whole read outlasts the
        // timeout, so it only passes if the timer re-arms per read. The 15x per-gap margin keeps
        // it stable when the full suite loads the thread pool.
        using var stream = new IdleTimeoutStream(new DrippingStream(TimeSpan.FromMilliseconds(50), 20),
            TimeSpan.FromMilliseconds(750));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var buffer = new byte[32];
        var total = 0;
        while (await stream.ReadAsync(buffer.AsMemory(total), cts.Token) > 0)
        {
            total++;
        }

        Assert.Equal(20, total);
    }

    [Fact]
    public async Task ExternalCancel_WinsOverIdleTimeout()
    {
        using var stream = new IdleTimeoutStream(new NeverendingStream(), TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // TaskCanceledException derives from OperationCanceledException.
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
#pragma warning disable CA2022 // The read is expected to throw (external cancel); the byte count is irrelevant.
            await stream.ReadAsync(new byte[16], cts.Token));
#pragma warning restore CA2022
    }

    [Fact]
    public void NonPositiveTimeout_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new IdleTimeoutStream(Stream.Null, TimeSpan.Zero));
    }

    [Fact]
    public async Task NonCancelableToken_StillSubjectToIdleTimeout()
    {
        // Covers the !CanBeCanceled fast path in ReadAsync: the idle timer alone
        // must be able to fail the read.
        using var stream = new IdleTimeoutStream(new NeverendingStream(), TimeSpan.FromMilliseconds(100));

        var exception = await Assert.ThrowsAsync<IOException>(async () =>
        {
#pragma warning disable CA2022 // The read is expected to throw (idle timeout); the byte count is irrelevant.
            await stream.ReadAsync(new byte[16], CancellationToken.None);
#pragma warning restore CA2022
        });

        Assert.Contains("idle", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SyncMembers_AreNotSupported()
    {
        using var stream = new IdleTimeoutStream(new NeverendingStream(), TimeSpan.FromSeconds(10));
        var buffer = new byte[16];

        Assert.Throws<NotSupportedException>(() => stream.Read(buffer, 0, buffer.Length));
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.Write(buffer, 0, buffer.Length));
        Assert.Throws<NotSupportedException>(() => stream.Flush());
        Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        Assert.Throws<NotSupportedException>(() => stream.Length);
        Assert.Throws<NotSupportedException>(() => stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Position = 0);
    }

    [Fact]
    public void DoubleDispose_IsIdempotent()
    {
        var inner = new DisposeCountingStream();
        var stream = new IdleTimeoutStream(inner, TimeSpan.FromSeconds(10));

        stream.Dispose();
        stream.Dispose(); // second dispose must not throw or re-dispose the inner stream

        Assert.Equal(1, inner.DisposeCount);
    }

    [Fact]
    public async Task ReadAfterDispose_ThrowsObjectDisposedException()
    {
        var stream = new IdleTimeoutStream(new NeverendingStream(), TimeSpan.FromSeconds(10));
        await stream.DisposeAsync();

#pragma warning disable CA2022 // The disposed stream must throw before reading any bytes.
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await stream.ReadAsync(new byte[1].AsMemory()));
#pragma warning restore CA2022
    }

    [Fact]
    public async Task DoubleDisposeAsync_IsIdempotent()
    {
        var inner = new DisposeCountingStream();
        var stream = new IdleTimeoutStream(inner, TimeSpan.FromSeconds(10));

        await stream.DisposeAsync();
        await stream.DisposeAsync();

        Assert.Equal(1, inner.DisposeAsyncCount);
    }

    private sealed class DisposeCountingStream : Stream
    {
        public int DisposeCount { get; private set; }
        public int DisposeAsyncCount { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(0);
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
        }

        public override ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class NeverendingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // Blocks forever unless cancelled — like a stalled network connection.
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class DrippingStream(TimeSpan delay, int count) : Stream
    {
        private int remaining = count;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);
            if (remaining-- <= 0)
            {
                return 0;
            }

            buffer.Span[0] = (byte)'x';
            return 1;
        }

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
