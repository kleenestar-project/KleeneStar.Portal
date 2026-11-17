using KleeneStar.Core.WebIdentity;

namespace KleeneStar.Portal.WebIdentity
{
    /// <summary>
    /// The session validator of the portal application (<see cref="SessionValidatorBase"/>).
    /// WebExpress discovers it in this assembly for the portal; the core's
    /// <see cref="SessionValidator"/> serves the core application only.
    /// </summary>
    public sealed class PortalSessionValidator : SessionValidatorBase
    {
    }
}
