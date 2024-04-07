using Spiffe.Bundle.X509;
using Spiffe.Id;

namespace Spiffe.Tests.Svid.X509;

internal class TestX509BundleSource(X509Bundle bundle) : IX509BundleSource
{
    public X509Bundle GetX509Bundle(TrustDomain trustDomain) => bundle;
}
