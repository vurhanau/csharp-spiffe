
using DotNext.Threading;

namespace Spiffe.WorkloadApi;

internal class Watcher : IX509ContextWatcher
{
    private IWorkloadApiClient _client;

    private Action<X509Context> _contextFn;

    private AsyncTrigger _contextSet;

    private AsyncTrigger _contextSetOnce;

    public void OnX509ContextUpdate(X509Context x509Context)
    {
    }

    public void OnX509ContextWatchError(Exception e)
    {
    }
}
