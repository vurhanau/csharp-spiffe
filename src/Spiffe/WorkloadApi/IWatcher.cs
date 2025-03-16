namespace Spiffe.WorkloadApi;

/// <summary>
///     Receives updates of <typeparamref name="T" /> from the Workload API.
/// </summary>
/// <typeparam name="T">Update type</typeparam>
public interface IWatcher<in T>
{
    /// <summary>
    ///     OnX509ContextUpdate is called with the latest X.509 context retrieved
    ///     from the Workload API.
    /// </summary>
    void OnUpdate(T update);

    /// <summary>
    ///     OnX509ContextWatchError is called when there is a problem establishing
    ///     or maintaining connectivity with the Workload API.
    /// </summary>
    void OnError(Exception e);
}
