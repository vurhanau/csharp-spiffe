using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Spiffe.Id;

namespace Spiffe.Tests.Util;

internal class CertificateCreationOptions
{
    public byte[]? SerialNumber { get; set; }

    public string? SubjectName { get; set; }

    public DateTimeOffset NotBefore { get; set; }

    public DateTimeOffset NotAfter { get; set; }

    public X509KeyUsageFlags KeyUsage { get; set; }

    public Uri? SubjectAlternateName { get; set; }
}

internal class CA
{
    internal static ECDsa CreateEC256Key()
    {
        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    internal static byte[] CreateSerial()
    {
        return BitConverter.GetBytes(Random.Shared.NextInt64());
    }

    internal static X509Certificate2 CreateSigningCertificate(X509Certificate2? parent = null)
    {
        using ECDsa key = CreateEC256Key();
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
            // set the AuthorityKeyIdentifier. There is no built-in
            // support, so it needs to be copied from the Subject Key
            // Identifier of the signing certificate and massaged slightly.
            // AuthorityKeyIdentifier is "KeyID=<subject key identifier>"
            byte[] issuerSubjectKey = parent.Extensions["Subject Key Identifier"]!.RawData;
            ArraySegment<byte> segment = new(issuerSubjectKey, 2, issuerSubjectKey.Length - 2);
            byte[] authorityKeyIdentifier = new byte[segment.Count + 4];

            // these bytes define the "KeyID" part of the AuthorityKeyIdentifer
            authorityKeyIdentifier[0] = 0x30;
            authorityKeyIdentifier[1] = 0x16;
            authorityKeyIdentifier[2] = 0x80;
            authorityKeyIdentifier[3] = 0x14;
            segment.CopyTo(authorityKeyIdentifier, 4);
            csr.CertificateExtensions.Add(new X509Extension("2.5.29.35", authorityKeyIdentifier, false));
        }

        // Create certs with SAN name in addition to the subject name
        // SubjectAlternativeNameBuilder sanBuilder = new();
        // sanBuilder.AddDnsName(subjectName);
        // X509Extension sanExtension = sanBuilder.Build();
        // csr.CertificateExtensions.Add(sanExtension);

        // Enhanced key usages
        // csr.CertificateExtensions.Add(
        //     new X509EnhancedKeyUsageExtension(
        //         [
        //             new Oid("1.3.6.1.5.5.7.3.2"), // TLS Client auth
        //             new Oid("1.3.6.1.5.5.7.3.1"), // TLS Server auth
        //         ],
        //         false));

        // add this subject key identifier
        csr.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(csr.PublicKey, false));

        DateTimeOffset notBefore = DateTimeOffset.UtcNow;
        DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddHours(1);
        if (parent == null)
        {
            return csr.CreateSelfSigned(notBefore, notAfter);
        }

        using X509Certificate2 ca = csr.Create(parent, notBefore, notAfter, serial);
        return ca.CopyWithPrivateKey(key);
    }

    internal static X509Certificate2 CreateX509Certificate(X509Certificate2 parent,
                                                           CertificateCreationOptions? options = null)
    {
        _ = parent ?? throw new ArgumentNullException(nameof(parent));
        if (!parent.HasPrivateKey)
        {
            throw new ArgumentException("Signing cert must have a private key");
        }

        byte[] serial = options?.SerialNumber ?? CreateSerial();
        using ECDsa key = CreateEC256Key();
        string subjectName = options?.SubjectName ?? $"CN=X509-Certificate {Convert.ToHexString(serial)}";
        CertificateRequest request = new(subjectName, key, HashAlgorithmName.SHA256);

        // set basic certificate contraints
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true));

        // key usage: Digital Signature and Key Encipherment
        X509KeyUsageFlags keyUsageFlags = options?.KeyUsage ?? X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment;
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                keyUsages: keyUsageFlags,
                critical: true));

        // set the AuthorityKeyIdentifier. There is no built-in
        // support, so it needs to be copied from the Subject Key
        // Identifier of the signing certificate and massaged slightly.
        // AuthorityKeyIdentifier is "KeyID=<subject key identifier>"
        byte[] issuerSubjectKey = parent.Extensions["X509v3 Subject Key Identifier"]!.RawData;
        ArraySegment<byte> segment = new(issuerSubjectKey, 2, issuerSubjectKey.Length - 2);
        byte[] authorityKeyIdentifer = new byte[segment.Count + 4];

        // these bytes define the "KeyID" part of the AuthorityKeyIdentifer
        authorityKeyIdentifer[0] = 0x30;
        authorityKeyIdentifer[1] = 0x16;
        authorityKeyIdentifer[2] = 0x80;
        authorityKeyIdentifer[3] = 0x14;
        segment.CopyTo(authorityKeyIdentifer, 4);
        request.CertificateExtensions.Add(new X509Extension(
            oid: "2.5.29.35",
            rawData: authorityKeyIdentifer,
            critical: false));

        // Create certs with a SAN name in addition to the subject name
        if (options?.SubjectAlternateName != null)
        {
            SubjectAlternativeNameBuilder sanBuilder = new();
            sanBuilder.AddUri(options.SubjectAlternateName);
            X509Extension sanExtension = sanBuilder.Build();
            request.CertificateExtensions.Add(sanExtension);
        }

        // Enhanced key usages
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [
                    new Oid("1.3.6.1.5.5.7.3.2"), // TLS Client auth
                    new Oid("1.3.6.1.5.5.7.3.1"), // TLS Server auth
                ],
                false));

        // add this subject key identifier
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        DateTimeOffset notBefore = DateTimeOffset.UtcNow;
        DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddHours(1);
        using X509Certificate2 cert = request.Create(parent, notBefore, notAfter, serial);

        return cert.CopyWithPrivateKey(key);
    }

    internal static X509Certificate2 CreateX509Svid(X509Certificate2 parent, SpiffeId id)
    {
        byte[] serial = CreateSerial();
        return CreateX509Certificate(parent, new()
        {
            SerialNumber = serial,
            SubjectName = $"CN=X509-SVID {Convert.ToHexString(serial)}",
            KeyUsage = X509KeyUsageFlags.DigitalSignature,
            SubjectAlternateName = id.ToUri(),
        });
    }
}
