﻿using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Spiffe.Bundle.X509;
using Spiffe.Id;
using Spiffe.Svid.X509;

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
    public TrustDomain? TrustDomain { get; set; }

    public CA? Parent { get; set; }

    public X509Certificate2? Cert { get; set; }

    internal static CA NewCA(TrustDomain trustDomain)
    {
        X509Certificate2 cert = CreateCACertificate();
        return new CA
        {
            TrustDomain = trustDomain,
            Cert = cert,
        };
    }

    internal CA ChildCA()
    {
        X509Certificate2 cert = CreateCACertificate(Cert);
        return new CA
        {
            Parent = this,
            Cert = cert,
        };
    }

    internal X509Svid CreateX509Svid(SpiffeId id)
    {
        X509Certificate2 cert = CreateX509Svid(Cert!, id);
        X509Certificate2Collection chain = [cert];
        chain.AddRange(Chain(false));
        return new(id, chain, string.Empty);
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

    internal X509Certificate2Collection Chain(bool includeRoot)
    {
        X509Certificate2Collection chain = [];
        CA? next = this;
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

    internal static X509Certificate2 CreateCACertificate(X509Certificate2? parent = null,
                                                         Action<CertificateRequest>? csrConfigure = null)
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
            X509Extension authorityKeyIdentifier = GetAuthorityKeyIdentifier(parent);
            csr.CertificateExtensions.Add(authorityKeyIdentifier);
        }

        csr.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(csr.PublicKey, false));

        DateTimeOffset notBefore = DateTimeOffset.UtcNow;
        DateTimeOffset notAfter = parent != null // Handles the case when cert.notAfter < issuer.notAfter
                                    ? DateTimeOffset.UtcNow.AddMinutes(30)
                                    : DateTimeOffset.UtcNow.AddHours(1);
        if (parent == null)
        {
            return csr.CreateSelfSigned(notBefore, notAfter);
        }

        csrConfigure?.Invoke(csr);

        using X509Certificate2 ca = csr.Create(parent, notBefore, notAfter, serial);
        return ca.CopyWithPrivateKey(key);
    }

    private static ECDsa CreateEC256Key()
    {
        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    private static byte[] CreateSerial()
    {
        return BitConverter.GetBytes(Random.Shared.NextInt64());
    }

    private static X509Certificate2 CreateX509Certificate(X509Certificate2 parent,
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

        X509Extension authorityKeyIdentifier = GetAuthorityKeyIdentifier(parent);
        request.CertificateExtensions.Add(authorityKeyIdentifier);

        // create certs with a SAN name in addition to the subject name
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
        DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddMinutes(30);
        using X509Certificate2 cert = request.Create(parent, notBefore, notAfter, serial);

        return cert.CopyWithPrivateKey(key);
    }

    private static X509Certificate2 CreateX509Svid(X509Certificate2 parent, SpiffeId id)
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

    private static X509Extension GetAuthorityKeyIdentifier(X509Certificate2 cert)
    {
        // There is no built-in support, so it needs to be copied from the
        // Subject Key Identifier of the signing certificate.
        // AuthorityKeyIdentifier is "KeyID=<subject key identifier>"
        byte[] issuerSubjectKey = cert.Extensions["X509v3 Subject Key Identifier"]!.RawData;
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
