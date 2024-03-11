using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Spiffe.Util;

/// <summary>
/// This class adds Go crypto capabilities to C#.
/// </summary>
internal static class Crypto
{
    /// <summary>
    /// Creates an X509 certificate with a private key.
    /// <br/>
    /// C# OID list: <seealso href="https://github.com/dotnet/runtime/blob/v8.0.1/src/libraries/Common/src/System/Security/Cryptography/Oids.cs"/>
    /// <br/>
    /// Go OID list: <seealso href="https://github.com/golang/go/blob/release-branch.go1.22/src/crypto/x509/x509.go#L462C1-L486C1"/>
    /// </summary>
    /// <param name="cert">Certificate</param>
    /// <param name="keyBytes">Private key data</param>
    /// <returns>Certificate with a private key</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="keyBytes"/> has an unknown key algorithm.</exception>
    internal static X509Certificate2 GetCertificateWithPrivateKey(X509Certificate2 cert, ReadOnlySpan<byte> keyBytes)
    {
        X509Certificate2 certWithPrivateKey;
        string ka = cert.GetKeyAlgorithm();
        switch (ka)
        {
            // RSA
            case "1.2.840.113549.1.1.1":
                {
                    using RSA rsa = RSA.Create();
                    rsa.ImportPkcs8PrivateKey(keyBytes, out int _);
                    certWithPrivateKey = cert.CopyWithPrivateKey(rsa);
                    break;
                }

            // DSA
            case "1.2.840.10040.4.1":
                {
                    using DSA dsa = DSA.Create();
                    dsa.ImportPkcs8PrivateKey(keyBytes, out int _);
                    certWithPrivateKey = cert.CopyWithPrivateKey(dsa);
                    break;
                }

            // ECDSA
            case "1.2.840.10045.2.1":
                {
                    using ECDsa ecdsa = ECDsa.Create();
                    ecdsa.ImportPkcs8PrivateKey(keyBytes, out int _);
                    certWithPrivateKey = cert.CopyWithPrivateKey(ecdsa);
                    break;
                }

            // ED25519
            // C# doesn't support it
            // Go spiffe: https://github.com/spiffe/go-spiffe/blob/04f99837aed10405d235e658f0874f807bc81fc7/v2/svid/x509svid/svid.go#L234
            case "1.3.101.112":
                {
                    throw new ArgumentException($"Unsupported key algorithm: '{ka}'");
                }

            default:
                {
                    throw new ArgumentException($"Unsupported key algorithm: '{ka}'");
                }
        }

        return certWithPrivateKey;
    }

    /// <summary>
    /// Parses one or more certificates from the given ASN.1 DER data.
    /// The certificates must be concatenated with no intermediate padding.
    /// See <seealso href="https://github.com/golang/go/blob/release-branch.go1.22/src/crypto/x509/parser.go#L1001C1-L1014C2"/>
    /// </summary>
    internal static X509Certificate2Collection ParseCertificates(ReadOnlySpan<byte> der)
    {
        X509Certificate2Collection certs = [];
        int offset = 0;
        while (offset < der.Length)
        {
            // If there are multiple certs are in blob - this code fails on MacOS for < .NET8.
            // https://github.com/dotnet/runtime/issues/82682
            ReadOnlySpan<byte> data = der[offset..];
            X509Certificate2 cert = new(data);
            certs.Add(cert);
            offset += cert.RawData.Length;
        }

        return certs;
    }
}
