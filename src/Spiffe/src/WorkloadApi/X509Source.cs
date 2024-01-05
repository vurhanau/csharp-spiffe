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
/// Implements the <see cref="IDisposable"/> interface to close the source,
/// dropping the connection to the Workload API. Other source methods will return an error
/// after close has been called.
/// </summary>
public sealed class X509Source : IX509Source
{
    private X509Svid _svid;

    private X509BundleSet _bundles;

    private readonly Func<List<X509Svid>, X509Svid>? _picker;

    private readonly IWorkloadApiClient _workloadApiClient;

    private readonly ReaderWriterLock _svidLock;

    private readonly ReaderWriterLock _bundlesLock;

    private readonly TimeSpan _lockTimeout;

    private volatile bool _disposed;

    private X509Source(Func<List<X509Svid>, X509Svid>? picker, IWorkloadApiClient workloadApiClient)
    {
        _picker = picker;
        _workloadApiClient = workloadApiClient;
        _svidLock = new ReaderWriterLock();
        _lockTimeout = TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc/>
    public X509Svid X509Svid => GetSvid();

    /// <summary>
    /// Instantiates X.509 source.
    /// </summary>
    public static IX509Source Create(X509SourceOptions options)
    {
        // TODO: add validation and fallbacks
        X509Source source = new X509Source(options?.SvidPicker, options!.WorkloadApiClient!);
        return source;
    }

    /// <inheritdoc/>
    public X509Bundle GetBundleForTrustDomain(TrustDomain trustDomain)
    {
        if (IsClosed())
        {
            throw new InvalidOperationException("X.509 bundle source is closed");
        }

        _bundlesLock.AcquireReaderLock(_lockTimeout);
        try
        {
            return _bundles.GetBundleForTrustDomain(trustDomain);
        }
        finally
        {
            _bundlesLock.ReleaseReaderLock();
        }
    }

    /// <inheritdoc/>
    public void Dispose() => throw new NotImplementedException();

    private bool IsClosed() => throw new NotImplementedException();

    private X509Svid GetSvid()
    {
        if (IsClosed())
        {
            throw new InvalidOperationException("X.509 SVID source is closed");
        }

        _svidLock.AcquireReaderLock(_lockTimeout);
        try
        {
            return _svid;
        }
        finally
        {
            _svidLock.ReleaseReaderLock();
        }
    }
}
