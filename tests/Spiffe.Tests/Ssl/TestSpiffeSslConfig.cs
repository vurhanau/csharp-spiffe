using System.Net.Security;
using FluentAssertions;
using Spiffe.Id;
using Spiffe.Ssl;
using Spiffe.Svid.X509;
using Spiffe.Tests.Helper;
using static Spiffe.Tests.Svid.X509.TestX509Verify;

namespace Spiffe.Tests.Ssl;

public class TestSpiffeSslConfig
{
    private static readonly TrustDomain s_td1 = TrustDomain.FromString("domain1.test");

    private static readonly SpiffeId s_workload1 = SpiffeId.FromPath(s_td1, "/workload1");

    private static readonly CA s_ca1 = CA.Create(s_td1);

    private static readonly TrustDomain s_td2 = TrustDomain.FromString("domain2.test");

    private static readonly SpiffeId s_workload2 = SpiffeId.FromPath(s_td2, "/workload2");

    private static readonly CA s_ca2 = CA.Create(TrustDomain.FromString("domain2.test"));

    [Fact]
    public void TestClientTlsConfig()
    {
        TestX509BundleSource bundles = new(s_ca1.X509Bundle());
        IAuthorizer any = Authorizers.AuthorizeAny();
        SslClientAuthenticationOptions opts = SpiffeSslConfig.GetTlsClientOptions(bundles, any);

        // Pass
        X509Svid svid1 = s_ca1.CreateX509Svid(s_workload1);
        bool authorized = opts.RemoteCertificateValidationCallback(this, svid1.Certificates[0], new(), SslPolicyErrors.None);
        authorized.Should().BeTrue();

        // Fail
        X509Svid svid2 = s_ca2.CreateX509Svid(s_workload2);
        authorized = opts.RemoteCertificateValidationCallback(this, svid2.Certificates[0], new(), SslPolicyErrors.None);
        authorized.Should().BeFalse();
    }
}
