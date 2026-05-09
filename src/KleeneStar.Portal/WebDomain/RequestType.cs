using System.Collections.Generic;

namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// Default <see cref="IRequestType"/> implementation. Built by the
    /// <see cref="WebManager.PortalManager"/> from the underlying <c>Class</c> projection.
    /// </summary>
    public sealed class RequestType : IRequestType
    {
        /// <inheritdoc/>
        public string Key { get; init; }

        /// <inheritdoc/>
        public string Title { get; init; }

        /// <inheritdoc/>
        public string Description { get; init; }

        /// <inheritdoc/>
        public string IconKey { get; init; }

        /// <inheritdoc/>
        public string Tone { get; init; }

        /// <inheritdoc/>
        public string Foreground { get; init; }

        /// <inheritdoc/>
        public IReadOnlyList<ITemplate> Templates { get; init; } = [];
    }
}
