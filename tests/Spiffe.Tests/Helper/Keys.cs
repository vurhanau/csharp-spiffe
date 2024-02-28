using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace Spiffe.Tests.Helper;

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

    public static bool EqualJwk(JsonWebKey k1, JsonWebKey k2)
    {
        string j1 = JsonSerializer.Serialize(k1);
        string j2 = JsonSerializer.Serialize(k2);
        return j1 == j2;
    }
}
