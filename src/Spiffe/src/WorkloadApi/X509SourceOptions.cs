using Spiffe.Svid.X509;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Options for creating a new <see cref="X509Source"/>.
/// </summary>
public class X509SourceOptions
{
    /// <summary>
    /// Gets an address to the Workload API, if it is not set, the default address will be used.
    /// </summary>
    public string? SpiffeSocketPath { get; init; }

    /// <summary>
    /// Gets timeout for initializing the instance.
    /// </summary>
    public TimeSpan InitTimeout { get; init; }

    /// <summary>
    /// Gets a function to choose the X.509 SVID from the list returned by the Workload API.
    /// If it is not set, the default SVID is picked.
    /// </summary>
    public Func<List<X509Svid>, X509Svid>? SvidPicker { get; init; }

    /// <summary>
    /// Gets a custom instance of a <see cref="IWorkloadApiClient"/>, if it is not set, a new client
    /// will be created.
    /// </summary>
    public IWorkloadApiClient? WorkloadApiClient { get; init; }
}
