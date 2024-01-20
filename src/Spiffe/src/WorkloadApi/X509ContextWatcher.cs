namespace Spiffe.WorkloadApi;

internal class X509ContextWatcher(Action<X509Context> contextFn) : IX509ContextWatcher
{
    private readonly Action<X509Context> _contextFn = contextFn;

    public void OnX509ContextUpdate(X509Context x509Context) => _contextFn(x509Context);

    public void OnX509ContextWatchError(Exception e)
    {
        // The watcher doesn't do anything special with the error. If logging is
        // desired, it should be provided to the Workload API client.
    }
}
