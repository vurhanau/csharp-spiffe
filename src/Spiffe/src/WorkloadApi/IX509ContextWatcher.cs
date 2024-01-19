namespace Spiffe.WorkloadApi;

/// <summary>
/// Receives X509Context updates from the Workload API.
/// </summary>
public interface IX509ContextWatcher
{
    /// <summary>
    /// OnX509ContextUpdate is called with the latest X.509 context retrieved
    /// from the Workload API.
    /// </summary>
    void OnX509ContextUpdate(X509Context x509Context);

    /// <summary>
    /// OnX509ContextWatchError is called when there is a problem establishing
    /// or maintaining connectivity with the Workload API.
    /// </summary>
    void OnX509ContextWatchError(Exception e);
}
