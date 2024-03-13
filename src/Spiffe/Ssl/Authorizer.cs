using Spiffe.Id;

namespace Spiffe.Ssl;

internal class Authorizer(Func<SpiffeId, bool> fn) : IAuthorizer
{
    private readonly Func<SpiffeId, bool> _fn = fn ?? throw new ArgumentNullException(nameof(fn));

    public bool Authorize(SpiffeId id) => _fn(id);
}
