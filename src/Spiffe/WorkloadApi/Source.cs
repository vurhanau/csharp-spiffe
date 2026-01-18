using Spiffe.Util;

namespace Spiffe.WorkloadApi
{
    /// <summary>
    /// Abstract base class for sources of SVIDs and bundles.
    /// </summary>
    public abstract class Source : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;

        private readonly TaskCompletionSource<bool> _initialized;

        private volatile int _disposed;

        private protected Source()
        {
            _lock = new ReaderWriterLockSlim();
            _initialized = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// Indicates if source is initialized.
        /// </summary>
        public virtual bool IsInitialized => _initialized.Task.IsCompletedSuccessfully;

        /// <summary>
        /// Indicates if the source has been disposed.
        /// </summary>
        protected bool IsDisposed => _disposed != 0;

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
        /// Gets the name of the source.
        /// </summary>
        protected virtual string SourceName { get; } = nameof(Source);

        /// <summary>
        /// Gets the initialization tasks of the source.
        /// </summary>
        protected virtual IEnumerable<Task> InitializationTasks => [_initialized.Task];

        /// <summary>
        /// Disposes the source, freeing underlying resources.
        /// After calling Dispose, other source methods will return an error.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0 && disposing)
            {
                _lock?.Dispose();
            }
        }

        /// <summary>
        /// Marks the source as initialized.
        /// </summary>
        protected virtual void Initialized()
        {
            if (!_initialized.Task.IsCompletedSuccessfully)
            {
                _initialized.SetResult(true);
            }
        }

        /// <summary>
        /// Waits until the source is updated or the <paramref name="cancellationToken"/> is cancelled or the timeout is reached.
        /// </summary>
        protected async Task WaitUntilUpdated(int timeoutMillis, CancellationToken cancellationToken = default)
        {
            await Wait.Until(
                SourceName,
                InitializationTasks,
                () => _initialized.SetResult(false),
                () => IsInitialized,
                () => IsDisposed,
                timeoutMillis,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the given operation while holding a read lock.
        /// Throws if the source is not initialized or has been disposed.
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        protected T ReadLocked<T>(Func<T> op)
        {
            Throws.IfNotInitialized(nameof(Source), IsInitialized);
            Throws.IfDisposed(nameof(Source), IsDisposed);

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
            Throws.IfDisposed(nameof(Source), IsDisposed);

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
    }
}
