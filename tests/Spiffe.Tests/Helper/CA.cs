using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using Spiffe.Bundle.Jwt;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.Jwt;
using Spiffe.Svid.X509;

namespace Spiffe.Tests.Helper;

internal class CertificateCreationOptions
{
    public byte[] SerialNumber { get; set; }

    public string SubjectName { get; set; }

    public DateTimeOffset NotBefore { get; set; }

    public DateTimeOffset NotAfter { get; set; }

    public X509KeyUsageFlags KeyUsage { get; set; }

    public Uri SubjectAlternateName { get; set; }
}

internal sealed class CA : IDisposable
{
    public TrustDomain TrustDomain { get; init; }

    public CA Parent { get; init; }

    public X509Certificate2 Cert { get; init; }

    public ECDsa JwtKey { get; init; }

    public string JwtKid { get; init; }

    public void Dispose()
    {
        Cert?.Dispose();
        JwtKey?.Dispose();
    }

    internal static CA Create(TrustDomain trustDomain)
    {
        return new CA
        {
            TrustDomain = trustDomain,
            Cert = CreateCACertificate(),
            JwtKey = Keys.CreateEC256Key(),
            JwtKid = Keys.GenerateKeyId(),
        };
    }

    internal CA ChildCA()
    {
        X509Certificate2 cert = CreateCACertificate(Cert);
        return new CA
        {
            Parent = this,
            Cert = cert,
            JwtKey = Keys.CreateEC256Key(),
            JwtKid = Keys.GenerateKeyId(),
        };
    }

    internal X509Svid CreateX509Svid(SpiffeId id)
    {
        X509Certificate2 cert = CreateX509Svid(Cert!, id);
        X509Certificate2Collection chain = [cert];
        chain.AddRange(Chain(false));
        return new(id, chain, string.Empty);
    }

    internal JwtSvid CreateJwtSvid(SpiffeId spiffeId, IEnumerable<string> audience, string hint = "")
    {
        DateTime expiry = DateTime.UtcNow.AddHours(1);
        List<Claim> claims = Jwt.GetClaims(spiffeId.Id, audience, expiry);
        string token = Jwt.Generate(claims, JwtKey, JwtKid);
        JwtSvid svid = JwtSvidParser.ParseInsecure(token, audience);
        return new JwtSvid(
            token: svid.Token,
            id: svid.Id,
            audience: audience,
            expiry: expiry,
            claims: claims.ToDictionary(c => c.Type, c => c.Value),
            hint: hint);
    }

    internal Dictionary<string, JsonWebKey> JwtAuthorities()
    {
        ECDsaSecurityKey ecdsa = new(JwtKey);
        JsonWebKey jwtKey = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(ecdsa);
        jwtKey.KeyId = JwtKid!;
        return new() { { JwtKid!,  jwtKey } };
    }

    internal X509Certificate2Collection X509Authorities()
    {
        CA root = this;
        while (root.Parent != null)
        {
            root = root.Parent;
        }

        X509Certificate2Collection result = [root.Cert!];
        return result;
    }

    internal X509Bundle X509Bundle()
    {
        return new X509Bundle(TrustDomain!, X509Authorities());
    }

    internal JwtBundle JwtBundle()
    {
        return new JwtBundle(TrustDomain!, JwtAuthorities());
    }

    internal X509Certificate2Collection Chain(bool includeRoot)
    {
        X509Certificate2Collection chain = [];
        CA next = this;
        while (next != null)
        {
            if (includeRoot || next.Parent != null)
            {
                chain.Add(next.Cert!);
            }

            next = next.Parent;
        }

        return chain;
    }

    internal static X509Certificate2 CreateCACertificate(X509Certificate2 parent = null,
                                                         Action<CertificateRequest> csrConfigure = null)
    {
        using ECDsa key = Keys.CreateEC256Key();
        byte[] serial = CreateSerial();
        string subjectName = $"CN=CA {Convert.ToHexString(serial)}";

        CertificateRequest csr = new(subjectName, key, HashAlgorithmName.SHA256);

        // set basic certificate contraints
        csr.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: true,
                hasPathLengthConstraint: false,
                pathLengthConstraint: -1,
                critical: true));

        // key usage
        csr.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign,
                true));

        if (parent != null)
        {
            X509Extension authorityKeyIdentifier = GetAuthorityKeyIdentifier(parent);
            csr.CertificateExtensions.Add(authorityKeyIdentifier);
        }

        csr.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(csr.PublicKey, false));

        DateTimeOffset notBefore = DateTimeOffset.UtcNow;
        DateTimeOffset notAfter = parent != null // Handles the case when cert.notAfter < issuer.notAfter
                                    ? parent.NotAfter.AddMinutes(-1)
                                    : notBefore.AddHours(1);
        if (parent == null)
        {
            return csr.CreateSelfSigned(notBefore, notAfter);
        }

        csrConfigure?.Invoke(csr);

        using X509Certificate2 ca = csr.Create(parent, notBefore, notAfter, serial);
        return ca.CopyWithPrivateKey(key);
    }

    internal static X509Certificate2 CreateX509Svid(X509Certificate2 parent,
                                                    SpiffeId id,
                                                    Action<CertificateCreationOptions> configure = null,
                                                    Action<CertificateRequest> configureCsr = null)
    {
        return CreateX509Certificate(parent,
                                     opts =>
                                     {
                                         byte[] serial = CreateSerial();
                                         opts.SerialNumber = serial;
                                         opts.SubjectName = $"CN=X509-SVID {Convert.ToHexString(serial)}";
                                         opts.KeyUsage = X509KeyUsageFlags.DigitalSignature;
                                         opts.SubjectAlternateName = id.ToUri();
                                         configure?.Invoke(opts);
                                     },
                                     configureCsr);
    }

    private static X509Certificate2 CreateX509Certificate(X509Certificate2 parent,
                                                          Action<CertificateCreationOptions> configureOptions = null,
                                                          Action<CertificateRequest> configureCsr = null)
    {
        _ = parent ?? throw new ArgumentNullException(nameof(parent));
        if (!parent.HasPrivateKey)
        {
            throw new ArgumentException("Signing cert must have a private key");
        }

        byte[] serial = CreateSerial();
        CertificateCreationOptions opts = new()
        {
            SerialNumber = serial,
            SubjectName = $"CN=X509-Certificate {Convert.ToHexString(serial)}",
            KeyUsage = X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            NotBefore = DateTimeOffset.UtcNow,
            NotAfter = parent.NotAfter.AddMinutes(-1),
        };
        configureOptions?.Invoke(opts);

        using ECDsa key = Keys.CreateEC256Key();
        CertificateRequest csr = new(opts.SubjectName, key, HashAlgorithmName.SHA256);

        // set basic certificate contraints
        csr.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true));

        // key usage: Digital Signature and Key Encipherment
        csr.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                keyUsages: opts.KeyUsage,
                critical: true));

        X509Extension authorityKeyIdentifier = GetAuthorityKeyIdentifier(parent);
        csr.CertificateExtensions.Add(authorityKeyIdentifier);

        // create certs with a SAN name in addition to the subject name
        if (opts.SubjectAlternateName != null)
        {
            SubjectAlternativeNameBuilder sanBuilder = new();
            sanBuilder.AddUri(opts.SubjectAlternateName);
            X509Extension sanExtension = sanBuilder.Build();
            csr.CertificateExtensions.Add(sanExtension);
        }

        // enhanced key usages
        csr.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [
                    new Oid("1.3.6.1.5.5.7.3.2"), // TLS Client auth
                    new Oid("1.3.6.1.5.5.7.3.1"), // TLS Server auth
                ],
                false));

        // add this subject key identifier
        csr.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(csr.PublicKey, false));

        configureCsr?.Invoke(csr);

        using X509Certificate2 cert = csr.Create(parent, opts.NotBefore, opts.NotAfter, serial);

        return cert.CopyWithPrivateKey(key);
    }

    private static byte[] CreateSerial()
    {
        return BitConverter.GetBytes(Random.Shared.NextInt64());
    }

    private static X509Extension GetAuthorityKeyIdentifier(X509Certificate2 cert)
    {
        // There is no built-in support, so it needs to be copied from the
        // Subject Key Identifier of the signing certificate.
        // AuthorityKeyIdentifier is "KeyID=<subject key identifier>"
        byte[] issuerSubjectKey = cert.Extensions.First(f => f.Oid?.Value == "2.5.29.14").RawData; // X509v3 Subject Key Identifier
        ArraySegment<byte> segment = new(issuerSubjectKey, 2, issuerSubjectKey.Length - 2);
        byte[] authorityKeyIdentifer = new byte[segment.Count + 4];

        // these bytes define the "KeyID" part of the AuthorityKeyIdentifer
        authorityKeyIdentifer[0] = 0x30;
        authorityKeyIdentifer[1] = 0x16;
        authorityKeyIdentifer[2] = 0x80;
        authorityKeyIdentifer[3] = 0x14;
        segment.CopyTo(authorityKeyIdentifer, 4);
        return new X509Extension(
            oid: "2.5.29.35",
            rawData: authorityKeyIdentifer,
            critical: false);
    }
}
