using System;
using System.Collections.Generic;

namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// The portal-visible projection of an <c>IObject</c> that arose from a portal
    /// submission. From a data-model perspective an issue is the same entity as the
    /// underlying object — surfaced under a different vocabulary and enriched with
    /// portal-only fields (<see cref="PortalState"/>, <see cref="SharedWith"/>,
    /// <see cref="Watchers"/>).
    /// </summary>
    public interface IIssue
    {
        /// <summary>
        /// Gets the human-readable issue key shown in lists and URIs (e.g. <c>INC-2041</c>).
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets the issue title. Maps to <c>Object.Summary</c>.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the long description of the issue. Maps to <c>Object.Description</c>.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the request type the issue was created against.
        /// </summary>
        IRequestType RequestType { get; }

        /// <summary>
        /// Gets the request-type display name (denormalized for list views).
        /// </summary>
        string RequestTypeName { get; }

        /// <summary>
        /// Gets the priority code — <c>P1</c> through <c>P4</c>.
        /// </summary>
        string Priority { get; }

        /// <summary>
        /// Gets the collapsed portal lifecycle state.
        /// </summary>
        PortalIssueState PortalState { get; }

        /// <summary>
        /// Gets the requester (originator) of the issue.
        /// </summary>
        IssueParticipant Requester { get; }

        /// <summary>
        /// Gets the assigned service group display name.
        /// </summary>
        string AssigneeLabel { get; }

        /// <summary>
        /// Gets a value indicating whether this issue requires an explicit approval step
        /// from the requester or an authorized cost-center owner.
        /// </summary>
        bool RequiresApproval { get; }

        /// <summary>
        /// Gets the timestamp at which the issue was created.
        /// </summary>
        DateTime Created { get; }

        /// <summary>
        /// Gets the timestamp at which the issue was last updated.
        /// </summary>
        DateTime Updated { get; }

        /// <summary>
        /// Gets the identities the issue has explicitly been shared with.
        /// </summary>
        IReadOnlyList<IssueParticipant> SharedWith { get; }

        /// <summary>
        /// Gets the identities subscribed to the issue's notifications.
        /// </summary>
        IReadOnlyList<IssueParticipant> Watchers { get; }

        /// <summary>
        /// Gets the conversation timeline (human comments + system notes).
        /// </summary>
        IReadOnlyList<IssueComment> Comments { get; }
    }
}
