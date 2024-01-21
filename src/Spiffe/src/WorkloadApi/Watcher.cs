namespace Spiffe.WorkloadApi;

// <inheritdoc/>
internal class Watcher<T> : IWatcher<T>
{
    private readonly Action<T> _onUpdate;

    private readonly Action<Exception> _onError;

    internal Watcher(Action<T> onUpdate, Action<Exception> onError)
    {
        _onUpdate = onUpdate;
        _onError = onError;
    }

    internal Watcher(Action<T> onUpdate)
    : this(onUpdate, _ => { })
    {
    }

    public void OnUpdate(T update) => _onUpdate(update);

    public void OnError(Exception e) => _onError(e);
}
