using KleeneStar.Portal.WebDomain;
using System;
using System.Collections.Generic;
using WebExpress.WebCore.WebComponent;

namespace KleeneStar.Portal.WebManager
{
    /// <summary>
    /// Defines the contract for orchestrating the portal-visible projection of the core
    /// object model. The manager composes <c>ObjectManager</c>, <c>ClassManager</c>,
    /// <c>FormManager</c> and the identity model into the portal-specific use cases:
    /// browsing the request-type catalog, listing issues, opening an issue, commenting,
    /// sharing, watching, and accepting/rejecting a proposed resolution.
    /// </summary>
    /// <remarks>
    /// Resolved through the portal's <see cref="PortalHub"/> via the standard
    /// <c>ComponentHub.GetComponentManager&lt;T&gt;()</c> pattern. Implementations should
    /// ensure thread-safety if used in a multi-threaded environment.
    /// </remarks>
    public interface IPortalManager : IComponentManager
    {
        /// <summary>An event that fires when a new issue has been submitted.</summary>
        event EventHandler<IIssue> IssueCreated;

        /// <summary>An event that fires when the portal-visible projection of an issue changes.</summary>
        event EventHandler<IIssue> IssueUpdated;

        /// <summary>An event that fires when a comment was added to an issue.</summary>
        event EventHandler<IssueComment> IssueCommented;

        /// <summary>An event that fires when one or more identities were added to an issue's shared-with list.</summary>
        event EventHandler<IIssue> IssueShared;

        /// <summary>An event that fires when an identity's share access was revoked.</summary>
        event EventHandler<IIssue> IssueUnshared;

        /// <summary>An event that fires when an identity subscribed to an issue's notifications.</summary>
        event EventHandler<IIssue> IssueWatched;

        /// <summary>An event that fires when an identity unsubscribed from an issue's notifications.</summary>
        event EventHandler<IIssue> IssueUnwatched;

        /// <summary>An event that fires when the service team proposed a resolution.</summary>
        event EventHandler<IIssue> IssueResolutionProposed;

        /// <summary>An event that fires when the requester accepted a proposed resolution.</summary>
        event EventHandler<IIssue> IssueResolutionAccepted;

        /// <summary>An event that fires when the requester rejected a proposed resolution.</summary>
        event EventHandler<IIssue> IssueResolutionRejected;

        /// <summary>An event that fires when the issue has been closed.</summary>
        event EventHandler<IIssue> IssueClosed;

        /// <summary>
        /// Gets the participant representing the currently authenticated portal user. The
        /// reference is provided by the <c>IdentityManager</c> on the operator side; in the
        /// initial mock implementation a fixed demo identity is returned.
        /// </summary>
        IssueParticipant CurrentUser { get; }

        /// <summary>
        /// Gets the directory of identities inside the active tenant — used by the share
        /// modal type-ahead and to resolve participant lookups in the issue list.
        /// </summary>
        /// <returns>The list of organization members visible to the calling identity.</returns>
        IReadOnlyList<IssueParticipant> GetOrganizationMembers();

        /// <summary>
        /// Returns the request types visible to the calling identity in the active
        /// workspace/tenant context.
        /// </summary>
        /// <returns>The request-type catalog.</returns>
        IReadOnlyList<IRequestType> GetRequestTypes();

        /// <summary>
        /// Returns a single request type by key.
        /// </summary>
        /// <param name="requestTypeKey">The stable request-type key.</param>
        /// <returns>The matching request type, or <c>null</c>.</returns>
        IRequestType GetRequestType(string requestTypeKey);

        /// <summary>
        /// Returns the issues visible to the calling identity in the requested scope.
        /// </summary>
        /// <param name="scope">The scope filter — <c>Mine</c> or <c>Organization</c>.</param>
        /// <returns>The list of issues, ordered by most-recently updated first.</returns>
        IReadOnlyList<IIssue> GetIssues(IssueScope scope);

        /// <summary>
        /// Returns a single issue by its human-readable key.
        /// </summary>
        /// <param name="issueKey">The issue key (e.g. <c>INC-2041</c>).</param>
        /// <returns>The matching issue, or <c>null</c> if not visible to the caller.</returns>
        IIssue GetIssue(string issueKey);

        /// <summary>
        /// Submits a new issue against the given request type and (optional) template.
        /// </summary>
        /// <param name="requestTypeKey">The request type to submit against.</param>
        /// <param name="templateKey">The template to use, or <c>null</c> for a blank request.</param>
        /// <param name="title">The short description of the issue.</param>
        /// <param name="description">The long description / steps to reproduce.</param>
        /// <param name="priority">The priority code (<c>P1</c>–<c>P4</c>).</param>
        /// <returns>The freshly persisted issue.</returns>
        IIssue CreateIssue(string requestTypeKey, string templateKey, string title, string description, string priority);

        /// <summary>
        /// Appends a comment to an issue's timeline.
        /// </summary>
        /// <param name="issueKey">The issue to comment on.</param>
        /// <param name="text">The message body.</param>
        /// <param name="visibility">The visibility flag (<c>public</c> or <c>internal-team</c>).</param>
        /// <returns>The updated issue.</returns>
        IIssue AddComment(string issueKey, string text, string visibility);

        /// <summary>
        /// Adds one or more identities of the same tenant to an issue's shared-with list.
        /// </summary>
        /// <param name="issueKey">The issue to share.</param>
        /// <param name="identityIds">The identity ids to add.</param>
        /// <returns>The updated issue.</returns>
        IIssue ShareIssue(string issueKey, IEnumerable<string> identityIds);

        /// <summary>
        /// Revokes a previously granted share from an issue.
        /// </summary>
        /// <param name="issueKey">The issue whose share is revoked.</param>
        /// <param name="identityId">The identity id whose share access is removed.</param>
        /// <returns>The updated issue.</returns>
        IIssue UnshareIssue(string issueKey, string identityId);

        /// <summary>
        /// Subscribes the calling identity to the issue's notifications.
        /// </summary>
        /// <param name="issueKey">The issue to watch.</param>
        /// <returns>The updated issue.</returns>
        IIssue Watch(string issueKey);

        /// <summary>
        /// Unsubscribes the calling identity from the issue's notifications.
        /// </summary>
        /// <param name="issueKey">The issue to stop watching.</param>
        /// <returns>The updated issue.</returns>
        IIssue Unwatch(string issueKey);

        /// <summary>
        /// Accepts a proposed resolution and closes the issue.
        /// </summary>
        /// <param name="issueKey">The issue whose resolution is accepted.</param>
        /// <returns>The updated issue.</returns>
        IIssue AcceptResolution(string issueKey);

        /// <summary>
        /// Rejects a proposed resolution and returns the issue to <see cref="PortalIssueState.InProgress"/>.
        /// </summary>
        /// <param name="issueKey">The issue whose resolution is rejected.</param>
        /// <param name="reason">The mandatory rejection reason.</param>
        /// <returns>The updated issue.</returns>
        IIssue RejectResolution(string issueKey, string reason);
    }
}
