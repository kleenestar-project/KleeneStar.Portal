using System;
using System.Collections.Generic;

namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// Default <see cref="IIssue"/> implementation. Materialized by the
    /// <see cref="WebManager.PortalManager"/> from the underlying <c>Object</c> entity
    /// plus its portal-side enrichment (sharing links, watcher links, projected state).
    /// </summary>
    public sealed class Issue : IIssue
    {
        /// <inheritdoc/>
        public string Key { get; init; }

        /// <inheritdoc/>
        public string Title { get; init; }

        /// <inheritdoc/>
        public string Description { get; init; }

        /// <inheritdoc/>
        public IRequestType RequestType { get; init; }

        /// <inheritdoc/>
        public string RequestTypeName { get; init; }

        /// <inheritdoc/>
        public string Priority { get; init; } = "P3";

        /// <inheritdoc/>
        public PortalIssueState PortalState { get; init; }

        /// <inheritdoc/>
        public IssueParticipant Requester { get; init; }

        /// <inheritdoc/>
        public string AssigneeLabel { get; init; }

        /// <inheritdoc/>
        public bool RequiresApproval { get; init; }

        /// <inheritdoc/>
        public DateTime Created { get; init; }

        /// <inheritdoc/>
        public DateTime Updated { get; init; }

        /// <inheritdoc/>
        public IReadOnlyList<IssueParticipant> SharedWith { get; init; } = [];

        /// <inheritdoc/>
        public IReadOnlyList<IssueParticipant> Watchers { get; init; } = [];

        /// <inheritdoc/>
        public IReadOnlyList<IssueComment> Comments { get; init; } = [];
    }
}
