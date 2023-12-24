namespace Spiffe.Svid.X509;

public class Svid
{
    public SpiffeId Id { get; init; }

    public IReadOnlyList<X509Certificate2> Certificates { get; init; }

    public byte[] PrivateKey { get; init; }

    public string Hint { get; init; }
}