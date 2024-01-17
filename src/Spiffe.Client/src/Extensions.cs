using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Spiffe.Client;

internal static class Extensions
{
    public static string ToDisplayString(this X509Chain chain)
    {
        var b = new StringBuilder();
        var chainPolicy = chain.ChainPolicy;
        b.AppendLine($"[{nameof(chainPolicy.RevocationFlag)}]");
        b.AppendLine($"  {chainPolicy.RevocationFlag}");
        b.AppendLine();
        b.AppendLine($"[{nameof(chainPolicy.RevocationMode)}]");
        b.AppendLine($"  {chainPolicy.RevocationMode}");
        b.AppendLine();
        b.AppendLine($"[{nameof(chainPolicy.VerificationFlags)}]");
        b.AppendLine($"  {chainPolicy.VerificationFlags}");
        b.AppendLine();
        b.AppendLine($"[{nameof(chainPolicy.VerificationTime)}]");
        b.AppendLine($"  {chainPolicy.VerificationTime}");
        b.AppendLine();
        if (chainPolicy.ApplicationPolicy.Count > 0)
        {
            b.AppendLine($"[Application Policy]");
            foreach (var policy in chainPolicy.ApplicationPolicy)
            {
                b.AppendLine($"  {policy.ToDisplayString()}");
            }

            b.AppendLine();
        }

        if (chainPolicy.CertificatePolicy.Count > 0)
        {
            b.AppendLine($"[Certificate Policy]");
            foreach (var policy in chainPolicy.CertificatePolicy)
            {
                b.AppendLine($"  {policy.ToDisplayString()}");
            }

            b.AppendLine();
        }

        var elements = chain.ChainElements.Cast<X509ChainElement>().Select((element, index) => (element, index));
        if (elements.Any())
        {
            b.AppendLine($"[Elements]");
            foreach (var (element, index) in elements)
            {
                b.AppendLine();
                b.AppendLine($"[Element {index + 1}]");
                b.AppendLine();
                b.Append(element.Certificate.ToString());
                b.AppendLine();
                b.AppendLine($"[Status]");
                foreach (var status in element.ChainElementStatus)
                {
                    b.AppendLine($"  {status.ToDisplayString()}");
                }
            }
        }

        return b.ToString();
    }

    public static string ToDisplayString(this Oid oid)
    {
        return oid.FriendlyName == oid.Value
            ? $"{oid.Value}"
            : $"{oid.FriendlyName}: {oid.Value}";
    }

    public static string ToDisplayString(this X509ChainStatus status)
    {
        return $"{status.Status}: {status.StatusInformation}";
    }
}
