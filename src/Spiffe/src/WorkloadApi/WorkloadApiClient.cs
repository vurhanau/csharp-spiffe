using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Grpc.Net.Client;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using static Spiffe.WorkloadApi.SpiffeWorkloadAPI;

namespace Spiffe.WorkloadApi;

/// <inheritdoc/>
public class WorkloadApiClient : IWorkloadApiClient
{
    private readonly GrpcChannel _channel;

    private readonly bool _disposeChannel;

    private readonly SpiffeWorkloadAPIClient _client;

    private bool _disposed = false;

    private static readonly Metadata Headers = new()
    {
        {
            "workload.spiffe.io", "true"
        },
    };

    private static readonly X509SVIDRequest X509SvidRequest = new();

    private WorkloadApiClient(GrpcChannel channel,
                              SpiffeWorkloadAPIClient client,
                              bool disposeChannel)
    {
        _channel = channel;
        _client = client;
        _disposeChannel = disposeChannel;
    }

    /// <summary>
    /// Creates a new Workload API client configured with the given client options.
    /// </summary>
    public static IWorkloadApiClient Create(GrpcChannel channel, bool dispose = true)
    {
        _ = channel ?? throw new ArgumentNullException(nameof(channel));

        SpiffeWorkloadAPIClient client = new(channel);
        return new WorkloadApiClient(channel, client, dispose);
    }

    /// <inheritdoc/>
    public async Task<X509Context> FetchX509ContextAsync(CancellationToken cancellationToken = default)
    {
        var call = _client.FetchX509SVID(X509SvidRequest, headers: Headers, cancellationToken: cancellationToken);
        await foreach (X509SVIDResponse resp in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            List<X509Svid> svids = new();
            Dictionary<SpiffeTrustDomain, X509Bundle> bundles = new();
            foreach (X509SVID svid in resp.Svids)
            {
                svids.Add(new X509Svid
                {
                    SpiffeId = SpiffeId.FromString(svid.SpiffeId),
                    Chain = new X509Chain(),
                    Certificate = new X509Certificate2(null),
                    Hint = svid.Hint,
                });
            }

            var ctx = new X509Context
            {
                X509Svids = svids,
                X509BundleSet = new X509BundleSet()
                {
                    Bundles = bundles,
                },
            };
        }
    }

    /// <inheritdoc/>
    public Task FetchX509BundlesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task WatchX509BundlesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task WatchX509ContextAsync(IWatcher<X509Context> watcher, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose(bool disposing) executes in two distinct scenarios.
    /// If disposing equals true, the method has been called directly
    /// or indirectly by a user's code. Managed and unmanaged resources
    /// can be disposed.
    /// If disposing equals false, the method has been called by the
    /// runtime from inside the finalizer and you should not reference
    /// other objects. Only unmanaged resources can be disposed.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && _disposeChannel)
            {
                _channel.Dispose();
            }

            _disposed = true;
        }
    }
}
