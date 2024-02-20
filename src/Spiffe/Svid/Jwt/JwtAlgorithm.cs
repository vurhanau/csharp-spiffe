namespace Spiffe.Svid.Jwt;

// Signature algorithms
internal static class JwtAlgorithm
{
    public const string EdDSA = "EdDSA";

    public const string HS256 = "HS256"; // HMAC using SHA-256

    public const string HS384 = "HS384"; // HMAC using SHA-384

    public const string HS512 = "HS512"; // HMAC using SHA-512

    public const string RS256 = "RS256"; // RSASSA-PKCS-v1.5 using SHA-256

    public const string RS384 = "RS384"; // RSASSA-PKCS-v1.5 using SHA-384

    public const string RS512 = "RS512"; // RSASSA-PKCS-v1.5 using SHA-512

    public const string ES256 = "ES256"; // ECDSA using P-256 and SHA-256

    public const string ES384 = "ES384"; // ECDSA using P-384 and SHA-384

    public const string ES512 = "ES512"; // ECDSA using P-521 and SHA-512

    public const string PS256 = "PS256"; // RSASSA-PSS using SHA256 and MGF1-SHA256

    public const string PS384 = "PS384"; // RSASSA-PSS using SHA384 and MGF1-SHA384

    public const string PS512 = "PS512"; // RSASSA-PSS using SHA512 and MGF1-SHA512
}
