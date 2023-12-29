namespace Spiffe.WorkloadApi;

/// <summary>
/// Watches updates of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Type of update.</typeparam>
public interface IWatcher<in T>
{
    /// <summary>
    /// Method called in case of success getting an update.
    /// </summary>
    Task OnUpdateAsync(T update, CancellationToken cancellationToken = default);

    /// <summary>
    /// Method called in case there is an error watching for updates.
    /// </summary>
    Task OnErrorAsync(Exception e, CancellationToken cancellationToken = default);
}
