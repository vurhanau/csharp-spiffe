using System.Security.Cryptography;

namespace Spiffe.Tests.Util;

internal static class Keys
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private const int KeyIdLength = 32;

    private static readonly ThreadLocal<Random> s_rand = new(() => new Random());

    public static ECDsa CreateEC256Key()
    {
        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    public static string GenerateKeyId()
    {
        return new string(Enumerable.Range(0, KeyIdLength).Select(_ =>
        {
            int r = s_rand.Value!.Next(KeyIdLength);
            return Alphabet[r];
        }).ToArray());
    }
}
