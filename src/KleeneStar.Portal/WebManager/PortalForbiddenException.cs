using System;

namespace KleeneStar.Portal.WebManager
{
    /// <summary>
    /// Thrown by the <see cref="IPortalManager"/> when the caller may see an issue but may not
    /// do what was asked of it - a colleague the issue was shared with accepting its resolution,
    /// a customer writing a note for the service team, an anonymous caller submitting a request.
    /// </summary>
    /// <remarks>
    /// The portal REST layer maps this to a <c>403 Forbidden</c> response. An issue the caller
    /// may not see at all is not a refusal but an absence: the manager answers it as
    /// <see langword="null"/> and the endpoint as <c>404</c>, so its existence is not disclosed.
    /// </remarks>
    public sealed class PortalForbiddenException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">A human-readable description of the refusal.</param>
        public PortalForbiddenException(string message)
            : base(message)
        {
        }
    }
}
