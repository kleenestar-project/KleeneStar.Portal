using System;

namespace KleeneStar.Portal.WebManager
{
    /// <summary>
    /// Thrown by the <see cref="IPortalManager"/> when a portal-only mutation collides
    /// with the current issue state — for example, accepting a resolution that has
    /// not been proposed, or unsubscribing from an issue the caller never watched.
    /// </summary>
    /// <remarks>
    /// The portal REST layer maps this to a <c>409 Conflict</c> response through
    /// <c>PortalApi.Conflict</c>.
    /// </remarks>
    public sealed class PortalConflictException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">A human-readable description of the conflict.</param>
        public PortalConflictException(string message)
            : base(message)
        {
        }
    }
}
