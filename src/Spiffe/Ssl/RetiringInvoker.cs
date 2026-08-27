using System.Net;
using System.Net.Http;

namespace Spiffe.Ssl;

internal sealed class RetiringInvoker : IDisposable
{
    private readonly HttpMessageInvoker _inner;

    private readonly CancellationTokenSource _abort = new();

    private readonly object _lock = new();

    private int _outstanding;

    private bool _retiring;

    private bool _disposed;

    internal RetiringInvoker(HttpMessageInvoker inner, DateTimeOffset credentialExpiry)
    {
        _inner = inner;
        CredentialExpiry = credentialExpiry;
    }

    internal DateTimeOffset CredentialExpiry { get; }

    internal async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _outstanding++;
        }

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abort.Token);
        try
        {
            HttpResponseMessage response = await _inner.SendAsync(request, linked.Token).ConfigureAwait(false);
            response.Content = new TrackedContent(response.Content, Release);
            return response;
        }
        catch
        {
            Release();
            throw;
        }
    }

    internal void Retire(DateTimeOffset deadline, TimeSpan minGrace)
    {
        bool dispose;
        lock (_lock)
        {
            if (_retiring || _disposed)
            {
                return;
            }

            _retiring = true;
            dispose = _outstanding == 0;
        }

        if (dispose)
        {
            Dispose();
            return;
        }

        TimeSpan delay = deadline - DateTimeOffset.UtcNow;
        if (delay < minGrace)
        {
            delay = minGrace;
        }

        _ = Task.Delay(delay).ContinueWith(
            _ => AbortAndDispose(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _abort.Dispose();
            _inner.Dispose();
        }
    }

    private void AbortAndDispose()
    {
        try
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                _abort.Cancel();
            }
        }
        finally
        {
            Dispose();
        }
    }

    private void Release()
    {
        bool dispose;
        lock (_lock)
        {
            _outstanding--;
            dispose = _retiring && _outstanding == 0;
        }

        if (dispose)
        {
            Dispose();
        }
    }

    private sealed class TrackedContent : HttpContent
    {
        private readonly HttpContent _inner;

        private readonly Action _release;

        private int _released;

        internal TrackedContent(HttpContent inner, Action release)
        {
            _inner = inner;
            _release = release;
            foreach (KeyValuePair<string, IEnumerable<string>> header in inner.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            if (_inner.Headers.ContentLength is long contentLength)
            {
                length = contentLength;
                return true;
            }

            length = 0;
            return false;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            try
            {
                await _inner.CopyToAsync(stream).ConfigureAwait(false);
            }
            catch
            {
                Release();
                throw;
            }
        }

        protected override async Task<Stream> CreateContentReadStreamAsync()
        {
            try
            {
                return await _inner.ReadAsStreamAsync().ConfigureAwait(false);
            }
            catch
            {
                Release();
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                Release();
            }

            base.Dispose(disposing);
        }

        private void Release()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                _release();
            }
        }
    }
}
