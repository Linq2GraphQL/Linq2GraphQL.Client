namespace Linq2GraphQL.Client.Subscriptions;

/// <summary>
///     Stream decorator that fails a read with an IOException once no bytes have
///     arrived within the idle timeout. Each read gets its own timer that runs only while
///     that read waits for data, so keep-alives (including WHATWG <c>:</c> comments, which
///     the SSE parser consumes invisibly) keep the stream alive, and time the consumer spends
///     between reads (processing an event, waiting for a thread) never counts as idle.
/// </summary>
internal sealed class IdleTimeoutStream : Stream
{
    private readonly Stream inner;
    private readonly TimeSpan idleTimeout;
    private bool disposed;

    internal IdleTimeoutStream(Stream inner, TimeSpan idleTimeout)
    {
        if (idleTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(idleTimeout), "Idle timeout must be positive.");
        }

        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.idleTimeout = idleTimeout;
    }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        // A fresh timer per read, disposed when the read completes: it runs only while waiting
        // for data, and a timer that fired during a read which still returned bytes cannot leak
        // into the next read (a CancellationTokenSource cannot be reset once cancelled).
        // CreateLinkedTokenSource with a non-cancelable token is equivalent to a plain source.
        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readCts.CancelAfter(idleTimeout);

        try
        {
            return await inner.ReadAsync(buffer, readCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            readCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Only the idle timer or the caller can cancel readCts; it fired and the caller did
            // not cancel, so the timer fired with no bytes received: the connection is dead. An
            // OperationCanceledException the inner stream raised on its own propagates unchanged.
            throw new IOException($"SSE idle timeout: no data received within {FormatTimeout(idleTimeout)}.");
        }
    }

    /// <summary>
    ///     Whole seconds as "N s", anything else as "N ms" - never rounds up (750 ms is not "1s");
    ///     a positive timeout under 1 ms is "&lt;1 ms", not "0 s". Expects a positive timeout, as
    ///     the constructor enforces.
    /// </summary>
    internal static string FormatTimeout(TimeSpan timeout)
    {
        var milliseconds = (long)timeout.TotalMilliseconds;
        if (milliseconds == 0)
        {
            return "<1 ms";
        }

        return milliseconds % 1000 == 0
            ? FormattableString.Invariant($"{milliseconds / 1000} s")
            : FormattableString.Invariant($"{milliseconds} ms");
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

        await inner.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
