namespace Spiffe.WorkloadApi
{
    /// <summary>
    /// Abstract base class for sources of SVIDs and bundles.
    /// </summary>
    public abstract class Source : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;

        private volatile int _initialized;

        private volatile int _disposed;

        private protected Source()
        {
            _lock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// Indicates if source is initialized.
        /// </summary>
        public virtual bool IsInitialized => _initialized == 1;

        private bool IsDisposed => _disposed != 0;

        /// <summary>
        /// Disposes the source, freeing underlying resources.
        /// After calling Dispose, other source methods will return an error.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the source, freeing underlying resources.
        /// After calling Dispose, other source methods will return an error.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                _lock.Dispose();
            }
        }

        /// <summary>
        /// Marks the source as initialized.
        /// </summary>
        protected virtual void Initialized() => _initialized = 1;

        /// <summary>
        /// Waits until the source is updated or the <paramref name="cancellationToken"/> is cancelled or the timeout is reached.
        /// </summary>
        protected async Task WaitUntilUpdated(int timeoutMillis, CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource timeout = new();
            timeout.CancelAfter(timeoutMillis);

            using CancellationTokenSource combined = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

            while (!IsInitialized &&
                   !IsDisposed &&
                   !combined.Token.IsCancellationRequested)
            {
                await Task.Delay(50, combined.Token)
                          .ConfigureAwait(false);
            }

            ThrowIfCancelled(cancellationToken);
            ThrowIfTimeout(timeout.Token, timeoutMillis);
            ThrowIfNotInitialized();
            ThrowIfDisposed();
        }

        /// <summary>
        /// Executes the given operation while holding a read lock.
        /// Throws if the source is not initialized or has been disposed.
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        protected T ReadLocked<T>(Func<T> op)
        {
            ThrowIfNotInitialized();
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                return op();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Executes the given operation while holding a write lock.
        /// Throws if the source has been disposed.
        /// </summary>
        protected void WriteLocked(Action op)
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                op();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private static void ThrowIfCancelled(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Source initialization was cancelled.", cancellationToken);
            }
        }

        private static void ThrowIfTimeout(CancellationToken timeout, int timeoutMillis)
        {
            if (timeout.IsCancellationRequested)
            {
                throw new TimeoutException($"Source was not initialized within the specified timeout of {timeoutMillis} milliseconds.");
            }
        }

        private void ThrowIfNotInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Source is not initialized");
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
        }
    }
}
