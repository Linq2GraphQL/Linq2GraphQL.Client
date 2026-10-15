namespace Linq2GraphQL.Client.Subscriptions;

/// <summary>
///     Stream decorator that fails a read with an IOException once no bytes have
///     arrived within the idle timeout. The timer is re-armed before every read, so
///     keep-alives (including WHATWG <c>:</c> comments, which the SSE parser consumes
///     invisibly) keep the stream alive.
/// </summary>
internal sealed class IdleTimeoutStream : Stream
{
    private readonly Stream inner;
    private readonly TimeSpan idleTimeout;
    private readonly CancellationTokenSource idleCts = new();
    private bool disposed;

    internal IdleTimeoutStream(Stream inner, TimeSpan idleTimeout)
    {
        if (idleTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(idleTimeout), "Idle timeout must be positive.");
        }

        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.idleTimeout = idleTimeout;
        idleCts.CancelAfter(idleTimeout);
    }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!cancellationToken.CanBeCanceled)
        {
            return await ReadInnerAsync(buffer, idleCts.Token, external: default).ConfigureAwait(false);
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(idleCts.Token, cancellationToken);
        return await ReadInnerAsync(buffer, linked.Token, external: cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<int> ReadInnerAsync(Memory<byte> buffer, CancellationToken readToken,
        CancellationToken external)
    {
        // Re-arm for this read: any bytes (data line, event boundary, comment) reset it.
        idleCts.CancelAfter(idleTimeout);

        try
        {
            return await inner.ReadAsync(buffer, readToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            idleCts.IsCancellationRequested &&
            (!external.CanBeCanceled || !external.IsCancellationRequested))
        {
            // The idle timer fired and the caller did not cancel: the connection is dead.
            // The linked readToken cannot be used to distinguish the sources — it is also
            // cancelled by the idle timer — so the caller's own token is checked here.
            throw new IOException($"SSE idle timeout: no data received within {idleTimeout.TotalSeconds:F0}s.");
        }
    }

    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        if (disposing)
        {
            idleCts.Dispose();
            inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        idleCts.Dispose();
        await inner.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
