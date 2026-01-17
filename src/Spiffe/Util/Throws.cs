namespace Spiffe.Util;

internal static class Throws
{
    internal static void IfNotInitialized(string caller, bool isInitialized)
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException($"{caller} is not initialized");
        }
    }

    internal static void IfDisposed(string caller, bool isDisposed)
    {
        ObjectDisposedException.ThrowIf(isDisposed, $"{caller} has been disposed");
    }

    internal static void IfCancelled(string caller, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException($"{caller} initialization was cancelled.", cancellationToken);
        }
    }

    internal static void IfTimeout(string caller, int timeoutMillis, CancellationToken timeout)
    {
        if (timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"{caller} was not initialized within the specified timeout of {timeoutMillis} milliseconds.");
        }
    }
}
