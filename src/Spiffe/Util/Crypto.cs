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
    /// On Windows, the private key is persisted to the certificate store so Schannel can access it.
    /// The caller should call <see cref="DeletePrivateKey"/> when done with the certificate to clean up.
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

        // On Windows, Schannel (responsible for the TLS handshake) requires private keys to be stored in a
        // persistant Key storage provider. The caller should dispose of the key as needed.
        if (OperatingSystem.IsWindows())
        {
            byte[] pfxBytes = certWithPrivateKey.Export(X509ContentType.Pkcs12);
            certWithPrivateKey.Dispose();
#if NET9_0_OR_GREATER
            certWithPrivateKey = X509CertificateLoader.LoadPkcs12(
                pfxBytes,
                null,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet);
#else
            certWithPrivateKey = new X509Certificate2(
                pfxBytes,
                (string?)null,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet);
#endif
            Array.Clear(pfxBytes, 0, pfxBytes.Length);
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
            X509Certificate2 cert = LoadCertificate(data);
            certs.Add(cert);
            offset += cert.RawData.Length;
        }

        return certs;
    }

    /// <summary>
    /// Deletes the persisted private key associated with a certificate (Windows only).
    /// This should be called when disposing certificates created with <see cref="GetCertificateWithPrivateKey"/>.
    /// </summary>
    /// <param name="cert">The certificate whose private key should be deleted</param>
    internal static void DeletePrivateKey(X509Certificate2 cert)
    {
        if (!OperatingSystem.IsWindows() || !cert.HasPrivateKey)
        {
            return;
        }

        try
        {
            string ka = cert.GetKeyAlgorithm();
            switch (ka)
            {
                case "1.2.840.113549.1.1.1": // RSA
                    {
                        RSA? rsaPivateKey = cert.GetRSAPrivateKey();
                        if (rsaPivateKey is not null)
                        {
                            DeleteRsaPrivateKey(rsaPivateKey);
                        }

                        break;
                    }

                case "1.2.840.10040.4.1": // DSA
                    {
                        DSA? dsaPrivateKey = cert.GetDSAPrivateKey();
                        if (dsaPrivateKey is not null)
                        {
                            DeleteDsaPrivateKey(dsaPrivateKey);
                        }

                        break;
                    }

                case "1.2.840.10045.2.1": // ECDSA
                    {
                        ECDsa? ecdsaPrivateKey = cert.GetECDsaPrivateKey();
                        if (ecdsaPrivateKey is not null)
                        {
                            DeleteEcdsaPrivateKey(ecdsaPrivateKey);
                        }

                        break;
                    }

                default:
                    // Unknown key algorithm - we don't know how to delete it, so skip cleanup
                    return;
            }
        }
        catch (Exception ex)
        {
            // If cleanup fails, do not block.
            _ = ex;
        }
    }

    private static void DeleteRsaPrivateKey(RSA rsa)
    {
        if (rsa is RSACryptoServiceProvider rsaCsp)
        {
            rsaCsp.PersistKeyInCsp = false;
            rsaCsp.Clear();
        }
        else if (rsa is RSACng rsaCng)
        {
            rsaCng.Key.Delete();
        }
    }

    private static void DeleteEcdsaPrivateKey(ECDsa ecdsa)
    {
        if (ecdsa is ECDsaCng ecdsaCng)
        {
            ecdsaCng.Key.Delete();
        }
    }

    private static void DeleteDsaPrivateKey(DSA dsa)
    {
        if (dsa is DSACryptoServiceProvider dsaCsp)
        {
            dsaCsp.PersistKeyInCsp = false;
            dsaCsp.Clear();
        }
        else if (dsa is DSACng dsaCng)
        {
            dsaCng.Key.Delete();
        }
    }

    private static X509Certificate2 LoadCertificate(ReadOnlySpan<byte> data)
    {
        #if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadCertificate(data);
        #else
            return new X509Certificate2(data);
        #endif
    }
}
