using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;
using Spiffe.WorkloadApi;

namespace Spiffe.Tests.Svid.X509;

internal sealed class TestX509Source(X509Bundle bundle, X509Svid svid) : IX509Source
{
    public X509Bundle GetX509Bundle(TrustDomain trustDomain) => bundle;

    public X509Svid GetX509Svid() => svid;

    public void Dispose()
    {
    }
}
