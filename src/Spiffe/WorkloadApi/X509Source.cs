using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;

namespace Spiffe.WorkloadApi;

/// <summary>
/// Represents a source of X.509 SVIDs and X.509 bundles maintained via the Workload API.
/// <br/>
/// It handles a <see cref="X509Svid"/> and a <see cref="X509BundleSet"/> that are updated automatically
/// whenever there is an update from the Workload API.
/// <br/>
/// Implements the <see cref="IDisposable"/> interface to close the source.
/// Other source methods will return an error after close has been called.
/// </summary>
public sealed class X509Source : Source, IX509Source
{
    private readonly Func<List<X509Svid>, X509Svid> _picker;

    private X509Svid? _svid;

    private X509BundleSet? _bundles;

    /// <summary>
    /// Constructs X509 source.
    /// Visible for testing.
    /// </summary>
    internal X509Source(Func<List<X509Svid>, X509Svid> picker)
        : base()
    {
        _picker = picker;
    }

    /// <summary>
    /// Creates a new <see cref="X509Source"/>. It awaits until the initial update
    /// has been received from the Workload API for <paramref name="timeoutMillis"/>. The source should be closed when
    /// no longer in use to free underlying resources.
    /// </summary>
    public static async Task<X509Source> CreateAsync(IWorkloadApiClient client,
                                                     Func<List<X509Svid>, X509Svid>? picker = null,
                                                     int timeoutMillis = 60_000,
                                                     CancellationToken cancellationToken = default)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));
        picker ??= GetDefaultSvid;

        X509Source source = new(picker);
        Watcher<X509Context> watcher = new(source.SetX509Context);
        _ = Task.Run(
            () => client.WatchX509ContextAsync(watcher, cancellationToken),
            cancellationToken);

        await source.WaitUntilUpdated(timeoutMillis, cancellationToken)
                    .ConfigureAwait(false);

        return source;
    }

    /// <summary>
    /// Gets a default SVID.
    /// </summary>
    public X509Svid GetX509Svid() => ReadLocked(() => _svid!);

    /// <summary>
    /// Gets a trust bundle associated with trust domain.
    /// </summary>
    public X509Bundle GetX509Bundle(TrustDomain trustDomain) => ReadLocked(() => _bundles!.GetX509Bundle(trustDomain));

    /// <summary>
    /// Visible for testing.
    /// </summary>
    internal void SetX509Context(X509Context x509Context)
    {
        WriteLocked(() =>
        {
            // Dispose the previous SVID, if any.
            _svid?.Dispose();
            _svid = _picker(x509Context.X509Svids);
            _bundles = x509Context.X509Bundles;
        });

        Initialized();
    }

    /// <summary>
    /// Visible for testing.
    /// </summary>
    internal static X509Svid GetDefaultSvid(List<X509Svid> svids)
    {
        if (svids == null || svids.Count == 0)
        {
            throw new ArgumentException("SVIDs must be non-empty");
        }

        return svids[0];
    }

    /// <summary>
    /// Cleans up any persisted private keys, needed for windows.
    /// </summary>
    public new void Dispose()
    {
        WriteLocked(() =>
        {
            _svid?.Dispose();
            _svid = null;
            _bundles = null;
        });
        base.Dispose();
    }
}
