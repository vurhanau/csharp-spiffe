namespace Spiffe.Util;

internal static class Wait
{
    internal static async Task Until(
        string caller,
        IEnumerable<Task> tasks,
        Action canceledFunc,
        Func<bool> isInitializedFunc,
        Func<bool> isDisposedFunc,
        int timeoutMillis,
        CancellationToken cancellationToken)
    {
        _ = tasks ?? throw new ArgumentNullException(nameof(tasks));
        _ = canceledFunc ?? throw new ArgumentNullException(nameof(canceledFunc));
        _ = isInitializedFunc ?? throw new ArgumentNullException(nameof(isInitializedFunc));
        _ = isDisposedFunc ?? throw new ArgumentNullException(nameof(isDisposedFunc));

        using CancellationTokenSource timeout = new();
        timeout.CancelAfter(timeoutMillis);

        using CancellationTokenSource combined = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

        try
        {
            await Task.WhenAll(tasks)
                      .WaitAsync(combined.Token)
                      .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            canceledFunc();
        }

        Throws.IfCancelled(caller, cancellationToken);
        Throws.IfTimeout(caller, timeoutMillis, timeout.Token);
        Throws.IfNotInitialized(caller, isInitializedFunc());
        Throws.IfDisposed(caller, isDisposedFunc());
    }
}
