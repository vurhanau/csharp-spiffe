using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Ssl;
using Spiffe.Svid.X509;
using Spiffe.Tests.Helper;
using Spiffe.Tests.Svid.X509;

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
    public void TestMtlsServerConfig()
    {
        X509Bundle b = s_ca1.X509Bundle();
        X509Svid svid = s_ca1.CreateX509Svid(s_workload1);
        TestX509Source source = new(b, svid);
        IAuthorizer any = Authorizers.AuthorizeAny();
        SslServerAuthenticationOptions opts = SpiffeSslConfig.GetMtlsServerOptions(source, any);
        opts.ClientCertificateRequired.Should().BeTrue();
        AssertRemoteValidationCallback(opts.RemoteCertificateValidationCallback);
        AssertSslContext(opts.ServerCertificateContext, svid.Certificates);
    }

    [Fact]
    public void TestServerMtlsConfigWithAuthorization()
    {
        X509Bundle b = s_ca1.X509Bundle();
        X509Svid serverSvid = s_ca1.CreateX509Svid(s_workload1);
        TestX509Source source = new(b, serverSvid);
        X509Svid clientSvid1 = s_ca1.CreateX509Svid(SpiffeId.FromPath(s_td1, "/client1"));
        X509Svid clientSvid2 = s_ca1.CreateX509Svid(SpiffeId.FromPath(s_td1, "/client2"));
        IAuthorizer any = Authorizers.AuthorizeId(clientSvid1.Id);

        SslServerAuthenticationOptions opts = SpiffeSslConfig.GetMtlsServerOptions(source, any);
        RemoteCertificateValidationCallback callback = opts.RemoteCertificateValidationCallback;
        callback(this, clientSvid1.Certificates[0], new(), SslPolicyErrors.None).Should().BeTrue();
        callback(this, clientSvid2.Certificates[0], new(), SslPolicyErrors.None).Should().BeFalse();
    }

    [Fact]
    public void TestServerMtlsConfigWithIntermediateCA()
    {
        X509Bundle b = s_ca1.X509Bundle();
        CA intermediateCA = s_ca1.ChildCA();
        X509Svid svid = intermediateCA.CreateX509Svid(s_workload1);
        TestX509Source source = new(b, svid);
        IAuthorizer any = Authorizers.AuthorizeAny();
        SslServerAuthenticationOptions opts = SpiffeSslConfig.GetMtlsServerOptions(source, any);
        opts.ClientCertificateRequired.Should().BeTrue();
        AssertRemoteValidationCallback(opts.RemoteCertificateValidationCallback);
        AssertSslContext(opts.ServerCertificateContext, svid.Certificates);
    }

    [Fact]
    public void TestServerMtlsConfigWithoutCertificates()
    {
        X509Bundle b = s_ca1.X509Bundle();
        X509Svid svid = s_ca1.CreateX509Svid(s_workload1);
        svid.Certificates.Clear();
        TestX509Source source = new(b, svid);
        IAuthorizer any = Authorizers.AuthorizeAny();
        Action f = () => SpiffeSslConfig.GetMtlsServerOptions(source, any);
        f.Should().Throw<ArgumentException>().WithMessage("SVID doesn't contain any certificates");
    }

    [Fact]
    public void TestClientMtlsConfig()
    {
        X509Bundle b = s_ca1.X509Bundle();
        X509Svid svid = s_ca1.CreateX509Svid(s_workload1);
        TestX509Source source = new(b, svid);
        IAuthorizer any = Authorizers.AuthorizeAny();
        SslClientAuthenticationOptions opts = SpiffeSslConfig.GetMtlsClientOptions(source, any);
        AssertRemoteValidationCallback(opts.RemoteCertificateValidationCallback);
        AssertSslContext(opts.ClientCertificateContext, svid.Certificates);
    }

    [Fact]
    public void TestClientMtlsConfigWithAuthorization()
    {
        X509Bundle b = s_ca1.X509Bundle();
        X509Svid serverSvid = s_ca1.CreateX509Svid(s_workload1);
        TestX509Source source = new(b, serverSvid);
        X509Svid clientSvid1 = s_ca1.CreateX509Svid(SpiffeId.FromPath(s_td1, "/client1"));
        X509Svid clientSvid2 = s_ca1.CreateX509Svid(SpiffeId.FromPath(s_td1, "/client2"));
        IAuthorizer any = Authorizers.AuthorizeId(clientSvid1.Id);

        SslClientAuthenticationOptions opts = SpiffeSslConfig.GetMtlsClientOptions(source, any);
        RemoteCertificateValidationCallback callback = opts.RemoteCertificateValidationCallback;
        callback(this, clientSvid1.Certificates[0], new(), SslPolicyErrors.None).Should().BeTrue();
        callback(this, clientSvid2.Certificates[0], new(), SslPolicyErrors.None).Should().BeFalse();
    }

    [Fact]
    public void TestClientTlsConfig()
    {
        IX509BundleSource bundles = new TestX509BundleSource(s_ca1.X509Bundle());
        SslClientAuthenticationOptions opts = SpiffeSslConfig.GetTlsClientOptions(bundles);
        AssertRemoteValidationCallback(opts.RemoteCertificateValidationCallback);
    }

    [Fact]
    public void TestServerTlsConfig()
    {
        X509Bundle b = s_ca1.X509Bundle();
        X509Svid svid = s_ca1.CreateX509Svid(s_workload1);
        TestX509Source source = new(b, svid);
        SslServerAuthenticationOptions opts = SpiffeSslConfig.GetTlsServerOptions(source);
        AssertSslContext(opts.ServerCertificateContext, svid.Certificates);
    }

    [Fact]
    public void TestServerTlsConfigWithIntermediateCA()
    {
        X509Bundle b = s_ca1.X509Bundle();
        CA intermediateCA = s_ca1.ChildCA();
        X509Svid svid = intermediateCA.CreateX509Svid(s_workload1);
        TestX509Source source = new(b, svid);
        SslServerAuthenticationOptions opts = SpiffeSslConfig.GetTlsServerOptions(source);
        AssertSslContext(opts.ServerCertificateContext, svid.Certificates);
    }

    [Fact]
    public void TestServerTlsConfigWithoutCertificates()
    {
        X509Bundle b = s_ca1.X509Bundle();
        X509Svid svid = s_ca1.CreateX509Svid(s_workload1);
        svid.Certificates.Clear();
        TestX509Source source = new(b, svid);
        Action f = () => SpiffeSslConfig.GetTlsServerOptions(source);
        f.Should().Throw<ArgumentException>().WithMessage("SVID doesn't contain any certificates");
    }

    private static void AssertRemoteValidationCallback(RemoteCertificateValidationCallback callback)
    {
        object sender = new();

        // Pass
        X509Svid svid1 = s_ca1.CreateX509Svid(s_workload1);
        bool authorized = callback(sender, svid1.Certificates[0], new(), SslPolicyErrors.None);
        authorized.Should().BeTrue();

        // Fail
        X509Svid svid2 = s_ca2.CreateX509Svid(s_workload2);
        authorized = callback(sender, svid2.Certificates[0], new(), SslPolicyErrors.None);
        authorized.Should().BeFalse();

        // Certificate is null - fail
        authorized = callback(sender, null, new(), SslPolicyErrors.None);
        authorized.Should().BeFalse();

        // Chain is null - fail
        authorized = callback(sender, svid1.Certificates[0], null, SslPolicyErrors.None);
        authorized.Should().BeFalse();
    }

    private static void AssertSslContext(SslStreamCertificateContext ctx, X509Certificate2Collection expected)
    {
        ctx.TargetCertificate.Should().Be(expected[0]);
        ctx.IntermediateCertificates.Should().Equal(expected.Skip(1));
    }
}
