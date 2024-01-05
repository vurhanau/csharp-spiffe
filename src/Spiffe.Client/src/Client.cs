using Grpc.Net.Client;
using Spiffe.Bundle.X509;
using Spiffe.Grpc;
using Spiffe.WorkloadApi;

namespace Spiffe.Client;

internal class Client : IWatcher<X509Context>, IWatcher<X509BundleSet>, IDisposable
{
    private readonly IWorkloadApiClient _client;

    private bool _disposed;

    private Client(IWorkloadApiClient client)
    {
        _client = client;
    }

    public async Task<X509Context> FetchX509Context(CancellationToken cancellationToken = default)
    {
        X509Context x509Context = await _client.FetchX509ContextAsync(cancellationToken);
        Print(x509Context);
        return x509Context;
    }

    public async Task<X509BundleSet> FetchX509Bundles(CancellationToken cancellationToken = default)
    {
        X509BundleSet x509Bundles = await _client.FetchX509BundlesAsync(cancellationToken);
        Print(x509Bundles);
        return x509Bundles;
    }

    public async Task WatchX509Context(CancellationToken cancellationToken = default)
    {
        await _client.WatchX509ContextAsync(this, cancellationToken);
    }

    public async Task WatchX509Bundles(CancellationToken cancellationToken = default)
    {
        await _client.WatchX509BundlesAsync(this, cancellationToken);
    }

    public Task OnUpdateAsync(X509Context update, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Update:");
        Print(update);
        return Task.CompletedTask;
    }

    public Task OnUpdateAsync(X509BundleSet update, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Update:");
        Print(update);
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception e, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Exception occurred:");
        Console.WriteLine(e);
        return Task.CompletedTask;
    }

    public static Client GetClient(string socketPath)
    {
        GrpcChannel channel = CreateChannel(socketPath);
        IWorkloadApiClient workloadApiClient = WorkloadApiClient.Create(channel, true);
        return new Client(workloadApiClient);
    }

    /// <summary>
    /// Disposes Workload API client.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _client.Dispose();
            }

            _disposed = true;
        }
    }

    private static void Print(X509Context x509Context)
    {
        var svids = x509Context.X509Svids;
        if (svids == null)
        {
            Console.WriteLine("X509 context is empty");
            return;
        }

        Console.WriteLine($"Received {svids.Count} svid(s)");
        Console.WriteLine();
        foreach (var svid in svids)
        {
            PrintField("SPIFFE ID", svid.SpiffeId?.Id);
            if (!string.IsNullOrEmpty(svid.Hint))
            {
                PrintField("Hint", svid.Hint);
            }

            PrintField($"Certificate", svid.Certificate?.ToString(true), false);
            PrintField("Bundle", svid.Chain?.ToDisplayString(), false);
        }
    }

    private static void Print(X509BundleSet x509BundleSet)
    {
        if (x509BundleSet.Bundles == null)
        {
            Console.WriteLine("X509 bundle set is empty");
            return;
        }

        Console.WriteLine("[Bundles]");
        foreach (var tdBundle in x509BundleSet.Bundles)
        {
            Console.WriteLine($"Trust domain: {tdBundle.Key}");
            X509Bundle bundle = tdBundle.Value;
            Console.WriteLine($"X509 certificate chain:");
            Console.WriteLine(bundle.Chain?.ToDisplayString());
        }
    }

    private static void PrintField(string key, string? value, bool indent = true)
    {
        var tab = indent ? "  " : string.Empty;
        Console.WriteLine($"[{key}]");
        Console.WriteLine($"{tab}{value}");
        Console.WriteLine();
    }

    private static GrpcChannel CreateChannel(string address)
    {
#if OS_WINDOWS
        return GrpcChannelFactory.CreateNamedPipeChannel(address);
#else
        return GrpcChannelFactory.CreateUnixSocketChannel(address);
#endif
    }
}
