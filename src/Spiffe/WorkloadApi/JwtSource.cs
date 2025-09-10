using Spiffe.Bundle.Jwt;
using Spiffe.Id;
using Spiffe.Svid.Jwt;

namespace Spiffe.WorkloadApi;

/// <inheritdoc/>
public sealed class JwtSource : Source, IJwtSource
{
    private readonly IWorkloadApiClient _client;

    private JwtBundleSet? _bundles;

    internal JwtSource(IWorkloadApiClient client)
        : base()
    {
        _client = client;
    }

    /// <inheritdoc/>
    public async Task<List<JwtSvid>> FetchJwtSvidsAsync(JwtSvidParams jwtParams, CancellationToken cancellationToken = default)
    {
        return await _client.FetchJwtSvidsAsync(jwtParams, cancellationToken)
                            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the JWT bundle for the given trust domain.
    /// </summary>
    public JwtBundle GetJwtBundle(TrustDomain trustDomain) => ReadLocked(() => _bundles!.GetJwtBundle(trustDomain));

    /// <summary>
    /// Creates a new <see cref="JwtSource"/>. It awaits until the initial update
    /// has been received from the Workload API for <paramref name="timeoutMillis"/>.
    /// The source should be closed when no longer in use to free underlying resources.
    /// </summary>
    public static async Task<JwtSource> CreateAsync(IWorkloadApiClient client,
                                                    int timeoutMillis = 60_000,
                                                    CancellationToken cancellationToken = default)
    {
        _ = client ?? throw new ArgumentNullException(nameof(client));

        JwtSource source = new(client);
        Watcher<JwtBundleSet> watcher = new(source.SetJwtBundleSet);
        _ = Task.Run(
            () => client.WatchJwtBundlesAsync(watcher, cancellationToken),
            cancellationToken);

        await source.WaitUntilUpdated(timeoutMillis, cancellationToken)
                    .ConfigureAwait(false);

        return source;
    }

    /// <summary>
    /// Visible for testing.
    /// </summary>
    internal void SetJwtBundleSet(JwtBundleSet jwtBundleSet)
    {
        WriteLocked(() =>
        {
            _bundles = jwtBundleSet;
            Initialized();
        });
    }
}
