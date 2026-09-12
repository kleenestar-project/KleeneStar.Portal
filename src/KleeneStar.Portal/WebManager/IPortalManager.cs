using KleeneStar.Model.Entities;
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
        /// Gets the directory of the organization <paramref name="callerId"/> belongs to: the
        /// active identities of the caller's tenant, the caller included, ordered by name.
        /// </summary>
        /// <remarks>
        /// An organization is a tenant. A caller that belongs to none - an operator-side account,
        /// or an identity the system does not know - has no organization to list and is answered
        /// with an empty directory, never with everyone: this list is what the share dialog
        /// offers, and sharing is bounded by the tenant (concept: <i>identities of the same
        /// tenant</i>).
        /// </remarks>
        /// <param name="callerId">The acting identity, or <see langword="null"/> for the fallback.</param>
        /// <returns>The organization members, or an empty list.</returns>
        IReadOnlyList<IssueParticipant> GetOrganizationMembers(Guid? callerId);

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
        /// Returns the issues visible to <paramref name="callerId"/> in the requested
        /// scope. Use this overload from REST endpoints and tests where the calling
        /// identity is known; production pages pass the authenticated identity
        /// resolved by the WebExpress session flow.
        /// </summary>
        /// <param name="scope">The scope filter — <c>Mine</c> or <c>Organization</c>.</param>
        /// <param name="callerId">
        /// The acting identity, or <see langword="null"/> to fall back to the seeded
        /// admin (the legacy behavior of the no-caller overload).
        /// </param>
        /// <returns>The list of issues, ordered by most-recently updated first.</returns>
        IReadOnlyList<IIssue> GetIssues(IssueScope scope, Guid? callerId);

        /// <summary>
        /// Returns a single issue by its human-readable key.
        /// </summary>
        /// <param name="issueKey">The issue key (e.g. <c>INC-2041</c>).</param>
        /// <returns>The matching issue, or <see langword="null"/> if not visible to the caller.</returns>
        IIssue GetIssue(string issueKey);

        /// <summary>
        /// Returns the workspaces the calling identity is allowed to administer from
        /// the portal. A tenant-bearing identity sees the workspaces that share one
        /// of its tenants; an operator-side identity (no tenant) sees every
        /// workspace.
        /// </summary>
        /// <returns>The ordered workspace list.</returns>
        IReadOnlyList<Workspace> GetWorkspaces();

        /// <summary>
        /// Returns the workspaces visible to <paramref name="callerId"/>; the
        /// production-page overload falls back to the seeded admin.
        /// </summary>
        /// <param name="callerId">The acting identity, or <see langword="null"/> for the fallback.</param>
        /// <returns>The ordered workspace list.</returns>
        IReadOnlyList<Workspace> GetWorkspaces(Guid? callerId);

        /// <summary>
        /// Returns a workspace by its slug key, or <see langword="null"/> when no
        /// workspace with the given key exists.
        /// </summary>
        /// <param name="workspaceKey">The workspace slug.</param>
        /// <returns>The workspace, or <see langword="null"/>.</returns>
        Workspace GetWorkspace(string workspaceKey);

        /// <summary>
        /// Returns the classes of the given workspace, ordered by name. Used by the
        /// portal's class-tile overview.
        /// </summary>
        /// <param name="workspaceId">The workspace id.</param>
        /// <returns>The ordered class list.</returns>
        IReadOnlyList<Class> GetClasses(Guid workspaceId);

        /// <summary>
        /// Returns a single class, or <see langword="null"/> when the id does not
        /// resolve to a class.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <returns>The class, or <see langword="null"/>.</returns>
        Class GetClass(Guid classId);

        /// <summary>
        /// Flips the <c>PortalVisible</c> flag of a class — a toggle between the two
        /// states. The endpoint is the simplest possible surface for the customer
        /// portal's "publish request type" action; full CRUD on classes stays with
        /// the operator WebApp.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <returns>The updated class, or <see langword="null"/> when the class is unknown.</returns>
        Class TogglePortalVisible(Guid classId);

        /// <summary>
        /// Returns the fields of the given class, ordered by name. Used by the
        /// portal's fields tab on the class detail page.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <returns>The active, non-deprecated fields.</returns>
        IReadOnlyList<Field> GetFields(Guid classId);

        /// <summary>
        /// Returns the active statuses of the given class, ordered by their display
        /// name.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <returns>The status list.</returns>
        IReadOnlyList<Status> GetStatuses(Guid classId);

        /// <summary>
        /// Returns the active priorities of the given class, ordered by their
        /// configured <c>Order</c> field.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <returns>The priority list.</returns>
        IReadOnlyList<Priority> GetPriorities(Guid classId);

        /// <summary>
        /// Returns the forms of the given class, ordered by name. The
        /// <c>PortalTemplate</c> flag identifies forms that surface as
        /// service-request templates.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <returns>The active forms.</returns>
        IReadOnlyList<Form> GetForms(Guid classId);

        /// <summary>
        /// Returns the active forms of the given class that are flagged as portal
        /// templates. Mirrors the helper used internally by the request-type
        /// projection.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <returns>The portal-template forms.</returns>
        IReadOnlyList<Form> GetPortalTemplates(Guid classId);

        /// <summary>
        /// Flips the <c>PortalTemplate</c> flag of a form — the per-form
        /// counterpart of <see cref="TogglePortalVisible"/>. Toggling is
        /// idempotent: calling twice restores the original state.
        /// </summary>
        /// <param name="formId">The form id.</param>
        /// <returns>The updated form, or <see langword="null"/> when the form is unknown.</returns>
        Form TogglePortalTemplate(Guid formId);

        /// <summary>
        /// Creates a new field on the given class. Used by the portal's add-form
        /// modal. The created field starts active and undeprecated; <c>Created</c>
        /// and <c>Updated</c> are stamped to <see cref="DateTime.UtcNow"/>.
        /// </summary>
        /// <param name="classId">The owning class id.</param>
        /// <param name="name">The field name (must be non-blank).</param>
        /// <param name="description">The optional long description.</param>
        /// <param name="fieldType">The field type.</param>
        /// <param name="cardinality">The cardinality.</param>
        /// <param name="required">Whether the field is required.</param>
        /// <param name="uniqueConstraint">Whether the field is unique.</param>
        /// <returns>The persisted field.</returns>
        Field AddField(Guid classId, string name, string description, FieldType fieldType, FieldCardinality cardinality, bool required, bool uniqueConstraint);

        /// <summary>
        /// Updates the editable properties of a field. The owning class is fixed.
        /// </summary>
        /// <param name="fieldId">The field id.</param>
        /// <param name="name">The new name.</param>
        /// <param name="description">The new description.</param>
        /// <param name="fieldType">The new field type.</param>
        /// <param name="cardinality">The new cardinality.</param>
        /// <param name="required">Whether the field is required.</param>
        /// <param name="uniqueConstraint">Whether the field is unique.</param>
        /// <returns>The updated field, or <see langword="null"/> when the field is unknown.</returns>
        Field UpdateField(Guid fieldId, string name, string description, FieldType fieldType, FieldCardinality cardinality, bool required, bool uniqueConstraint);

        /// <summary>
        /// Creates a copy of the field with a new unique name (suffix
        /// <c>" (copy)"</c> when the original name is still available). The clone
        /// starts active.
        /// </summary>
        /// <param name="fieldId">The source field id.</param>
        /// <returns>The cloned field, or <see langword="null"/> when the source is unknown.</returns>
        Field CloneField(Guid fieldId);

        /// <summary>
        /// Marks a field as deprecated (soft delete). Deprecated fields are hidden
        /// from the portal's list projection; the underlying row is preserved for
        /// audit.
        /// </summary>
        /// <param name="fieldId">The field id.</param>
        /// <returns><see langword="true"/> when the field existed and was deprecated.</returns>
        bool DeleteField(Guid fieldId);

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
        /// <param name="visibility">
        /// The visibility flag (<c>public</c> or <c>internal-team</c>). It is persisted on
        /// the comment and narrows the timeline the portal projects: an
        /// <c>internal-team</c> comment reaches the assigned service group plus the
        /// requester, never the identities the issue is merely shared with. An
        /// unrecognised value reads as <c>public</c>.
        /// </param>
        /// <returns>The updated issue, whose timeline is filtered for the caller.</returns>
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
        /// <exception cref="PortalConflictException">
        /// Thrown when the issue is not in <see cref="PortalIssueState.Resolved"/> (the
        /// concept document's 409 Conflict on accepting / rejecting a non-proposed
        /// resolution).
        /// </exception>
        IIssue RejectResolution(string issueKey, string reason);

        /// <summary>
        /// Fires the <see cref="IssueResolutionProposed"/> event for an issue whose
        /// workflow status has just been stamped with a Done-category, non-terminal
        /// status by the operator workflow. No-op when the object is unknown, when the
        /// object's class is not portal-visible, or when the new state does not
        /// collapse to <see cref="PortalIssueState.Resolved"/>.
        /// </summary>
        /// <param name="objectId">The underlying object id.</param>
        /// <returns>The projected issue when the event was raised, <see langword="null"/> otherwise.</returns>
        IIssue NotifyResolutionProposed(Guid objectId);

        /// <summary>
        /// Subscribes the portal to the operator-side events it derives its own from: a workflow
        /// transition that stamps a portal-visible object with a resolved state raises
        /// <see cref="IssueResolutionProposed"/> through <see cref="NotifyResolutionProposed"/>.
        /// Called once when the portal application starts; calling it again does nothing.
        /// </summary>
        void Connect();
    }
}
