using System.Security.Cryptography.X509Certificates;
using Spiffe.Id;

namespace Spiffe.Ssl;

internal class Authorizer : IAuthorizer
{
    private readonly Func<SpiffeId, bool> _fn;

    public Authorizer(Func<SpiffeId, bool> fn)
    {
        _fn = fn ?? throw new ArgumentNullException(nameof(fn));
    }

    public bool Authorize(SpiffeId id)
    {
        return _fn(id);
    }
}
