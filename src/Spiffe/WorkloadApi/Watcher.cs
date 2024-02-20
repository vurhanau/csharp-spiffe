namespace Spiffe.WorkloadApi;

// <inheritdoc/>
public class Watcher<T> : IWatcher<T>
{
    private readonly Action<T> _onUpdate;

    private readonly Action<Exception> _onError;

    /// <summary>
    /// Constructor
    /// </summary>
    public Watcher(Action<T> onUpdate, Action<Exception> onError)
    {
        _onUpdate = onUpdate ?? throw new ArgumentNullException(nameof(onUpdate));
        _onError = onError ?? throw new ArgumentNullException(nameof(onError));
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Watcher(Action<T> onUpdate)
    : this(onUpdate, _ => { })
    {
    }

    /// <inheritdoc/>
    public void OnUpdate(T update) => _onUpdate(update);

    /// <inheritdoc/>
    public void OnError(Exception e) => _onError(e);
}
