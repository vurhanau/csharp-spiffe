namespace Spiffe.Svid.Jwt;

// Signature algorithms
internal static class JwtAlgorithm
{
    public const string Rs256 = "RS256"; // RSASSA-PKCS-v1.5 using SHA-256

    public const string Rs384 = "RS384"; // RSASSA-PKCS-v1.5 using SHA-384

    public const string Rs512 = "RS512"; // RSASSA-PKCS-v1.5 using SHA-512

    public const string Es256 = "ES256"; // ECDSA using P-256 and SHA-256

    public const string Es384 = "ES384"; // ECDSA using P-384 and SHA-384

    public const string Es512 = "ES512"; // ECDSA using P-521 and SHA-512

    public const string Ps256 = "PS256"; // RSASSA-PSS using SHA256 and MGF1-SHA256

    public const string Ps384 = "PS384"; // RSASSA-PSS using SHA384 and MGF1-SHA384

    public const string Ps512 = "PS512"; // RSASSA-PSS using SHA512 and MGF1-SHA512
}
