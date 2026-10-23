using KleeneStar.Core;
using KleeneStar.Core.WebManager;
using KleeneStar.Core.WebParameter;
using KleeneStar.Core.WebPermission;
using KleeneStar.Core.WebPermissions;
using KleeneStar.Model.Entities;
using KleeneStar.Portal.WebDomain;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using WebExpress.WebCore;
using WebExpress.WebCore.WebComponent;
using WebExpress.WebIndex.Queries;

namespace KleeneStar.Portal.WebManager
{
    // The entity type Object collides with System.Object and the entity type Template
    // with the portal's Template projection; alias both so the projection code reads
    // naturally.
    using ObjectEntity = KleeneStar.Model.Entities.Object;
    using TemplateProjection = KleeneStar.Portal.WebDomain.Template;

    /// <summary>
    /// Default <see cref="IPortalManager"/> implementation. The manager owns no data of
    /// its own — it composes the operator-side managers exposed on <see cref="CoreHub"/>
    /// (<c>ClassManager</c>, <c>ObjectManager</c>, <c>FormManager</c>, <c>ValueManager</c>,
    /// <c>CommentManager</c>, <c>WatcherManager</c>, <c>ShareManager</c>,
    /// <c>IdentityManager</c>, …) into the portal-specific use cases: browsing the
    /// request-type catalog, listing and opening issues, commenting, sharing, watching,
    /// and accepting/rejecting a proposed resolution.
    /// </summary>
    /// <remarks>
    /// The portal-visible projection follows <c>docs/kleenestar.portal.md</c>:
    /// request types are <see cref="Class"/> entities flagged
    /// <see cref="Class.PortalVisible"/>, templates are <see cref="Form"/> entities
    /// flagged <see cref="Form.PortalTemplate"/>, issues are <see cref="ObjectEntity"/>
    /// rows of portal-visible classes, and the collapsed
    /// <see cref="PortalIssueState"/> is derived from the object's workflow-field value
    /// via the status's <see cref="StatusCategory"/>.
    /// <para>
    /// <b>Who acts is the session's caller.</b> Every use case answers for the identity
    /// <c>ISessionManager.GetCurrentIdentityId</c> names on this call chain - the signed-in
    /// user of the request, or the identity a <c>BeginIdentity</c> scope stands in with. There
    /// is no stand-in identity: an anonymous caller sees no issue and changes nothing. Only
    /// with nobody acting at all (start-up, background work) does the manager answer as the
    /// system, which sees every issue.
    /// </para>
    /// <para>
    /// <b>The portal has its own access rule</b> (<see cref="IssueAccess"/>), so its reads of
    /// the objects lift the operator-side narrowing and apply that rule instead: the requester
    /// and the identities an issue was shared with always see it; the members of the
    /// requester's organization (tenant) see it while it is not restricted to its participants
    /// and its security level clears them; an operator-side account (no tenant) sees what the
    /// operator side lets it read. The workspace grants of the operator side are no answer
    /// here - a customer is not given read access to a service desk, yet must see the request
    /// they filed in it.
    /// </para>
    /// </remarks>
    public sealed class PortalManager : IPortalManager
    {
        private readonly IComponentHub _componentHub;
        private readonly IHttpServerContext _httpServerContext;

        /// <summary>
        /// Serializes issue creation so the per-workspace key allocation cannot hand out the
        /// same key twice.
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Whether <see cref="Connect"/> has subscribed to the operator-side events already.
        /// </summary>
        private bool _connected;

        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueCreated;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueUpdated;
        /// <inheritdoc/>
        public event EventHandler<IssueComment> IssueCommented;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueShared;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueUnshared;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueWatched;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueUnwatched;
        /// <inheritdoc/>
        // Raised when the operator side proposes a resolution: the underlying object
        // is stamped with a Done-category, non-terminal status (e.g. "Resolved",
        // "Awaiting Confirmation") and the issue becomes the user-facing "Resolved"
        // state. The portal itself only accepts or rejects, so this event follows the
        // operator-side workflow: Connect() listens to the workflow manager's
        // TransitionExecuted and hands every completed move to
        // NotifyResolutionProposed, which decides whether it was a proposal.
        public event EventHandler<IIssue> IssueResolutionProposed;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueResolutionAccepted;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueResolutionRejected;
        /// <inheritdoc/>
        public event EventHandler<IIssue> IssueClosed;

        /// <summary>
        /// Initializes a new instance of the class. Invoked by WebExpress via reflection.
        /// </summary>
        /// <param name="componentHub">The component hub.</param>
        /// <param name="httpServerContext">The reference to the context of the host.</param>
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used via Reflection.")]
        private PortalManager(IComponentHub componentHub, IHttpServerContext httpServerContext)
        {
            _componentHub = componentHub;
            _httpServerContext = httpServerContext;
        }

        /// <inheritdoc/>
        public IssueParticipant CurrentUser
        {
            get
            {
                var caller = Caller;

                return caller == Guid.Empty ? null : ToParticipant(CoreHub.IdentityManager.GetIdentity(caller));
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IssueParticipant> GetOrganizationMembers()
        {
            return GetOrganizationMembers(null);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IssueParticipant> GetOrganizationMembers(Guid? callerId)
        {
            var me = callerId ?? Caller;
            var caller = me == Guid.Empty ? null : CoreHub.IdentityManager.GetIdentity(me);

            // no tenant, no organization: an operator-side account has no colleagues in the
            // portal's sense, and an unknown caller is not told who works where
            if (caller?.TenantId is not Guid tenantId)
            {
                return [];
            }

            return [.. CoreHub.IdentityManager
                .GetIdentities(new Query<Identity>())
                .Where(i => i.State == IdentityState.Active && i.TenantId == tenantId)
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ToParticipant)];
        }

        /// <inheritdoc/>
        public IReadOnlyList<IRequestType> GetRequestTypes()
        {
            var viewer = Viewer.Resolve(null);

            return [.. GetPortalClasses().Where(viewer.Offers).Select(BuildRequestType)];
        }

        /// <inheritdoc/>
        public IRequestType GetRequestType(string requestTypeKey)
        {
            if (string.IsNullOrWhiteSpace(requestTypeKey))
            {
                return null;
            }

            return GetRequestTypes()
                .FirstOrDefault(r => string.Equals(r.Key, requestTypeKey, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public IReadOnlyList<IIssue> GetIssues(IssueScope scope)
        {
            return GetIssues(scope, null);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IIssue> GetIssues(IssueScope scope, Guid? callerId)
        {
            var viewer = Viewer.Resolve(callerId);

            if (!viewer.IsSystem && viewer.Id == Guid.Empty)
            {
                return [];
            }

            // the organization is a tenant: an operator-side account belongs to none and has no
            // organization scope to list
            if (scope == IssueScope.Organization && !viewer.IsSystem && viewer.TenantId is null)
            {
                return [];
            }

            var classes = GetPortalClasses();
            var requestTypes = classes.ToDictionary(cls => cls.Id, BuildRequestType);

            // load the share/watch relations once and group them by object so the
            // per-issue projection does not issue one query per row.
            var sharesByObject = CoreHub.ShareManager
                .GetShares(new Query<ObjectShare>())
                .GroupBy(s => s.ObjectId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var watchersByObject = CoreHub.WatcherManager
                .GetWatchers(new Query<ObjectWatcher>())
                .GroupBy(w => w.ObjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var issues = new List<IIssue>();

            foreach (var cls in classes)
            {
                var context = BuildClassContext(cls);
                var objects = ReadObjects(new Query<ObjectEntity>().WhereEquals(x => x.ClassId, cls.Id));
                var operatorReadable = viewer.IsOperator ? viewer.ReadableIds(cls.Id) : null;

                foreach (var entity in objects)
                {
                    sharesByObject.TryGetValue(entity.Id, out var shares);
                    watchersByObject.TryGetValue(entity.Id, out var watchers);

                    var access = viewer.AccessTo(entity, shares, operatorReadable);

                    if (access == IssueAccess.None)
                    {
                        continue;
                    }

                    if (scope == IssueScope.Mine && !viewer.IsSystem)
                    {
                        var isMine = access is IssueAccess.Requester or IssueAccess.Participant
                            || (watchers?.Any(w => w.IdentityId == viewer.Id) ?? false);

                        if (!isMine)
                        {
                            continue;
                        }
                    }
                    else if (scope == IssueScope.Organization && !viewer.IsSystem)
                    {
                        // the organization's issues are the ones its members filed; a request of
                        // another tenant in a workspace both are served by is not part of it
                        if (viewer.TenantOf(entity.CreatorId) != viewer.TenantId)
                        {
                            continue;
                        }
                    }

                    issues.Add(BuildIssue(entity, context, requestTypes[cls.Id], shares, watchers, comments: null, viewer: viewer));
                }
            }

            return [.. issues.OrderByDescending(i => i.Updated)];
        }

        /// <inheritdoc/>
        public IIssue GetIssue(string issueKey)
        {
            var viewer = Viewer.Resolve(null);
            var entity = ResolveIssueObject(issueKey, viewer, out var cls, out _);

            return entity is null ? null : BuildIssue(entity, cls, viewer);
        }

        /// <inheritdoc/>
        public IIssue CreateIssue(string requestTypeKey, string templateKey, string title, string description, string priority, bool restricted = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            var viewer = Viewer.Resolve(null);

            if (!viewer.IsSystem && viewer.Id == Guid.Empty)
            {
                throw new PortalForbiddenException("Sign in to submit a request.");
            }

            lock (_gate)
            {
                // a request type the caller is not offered does not exist for them, whatever its
                // key - a customer cannot file into a workspace that does not serve their tenant
                var cls = GetPortalClasses()
                    .Where(viewer.Offers)
                    .FirstOrDefault(c => string.Equals(RequestTypeKey(c), Slug(requestTypeKey), StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Request type '{requestTypeKey}' not found.");

                var context = BuildClassContext(cls);

                // a template is a portal-flagged form of the class; it currently
                // contributes its description as a prefill when none was supplied.
                var template = string.IsNullOrWhiteSpace(templateKey)
                    ? null
                    : context.Templates.FirstOrDefault(f => string.Equals(Slug(f.Name), Slug(templateKey), StringComparison.OrdinalIgnoreCase));

                var workspace = CoreHub.WorkspaceManager.GetWorkspace(cls.WorkspaceId);
                Guid? me = viewer.Id == Guid.Empty ? null : viewer.Id;
                var now = DateTime.UtcNow;

                var entity = new ObjectEntity
                {
                    Key = CoreHub.ObjectManager.NextObjectKey(cls.WorkspaceId) ?? NextFallbackKey(workspace),
                    Summary = title,
                    Description = string.IsNullOrWhiteSpace(description)
                        ? template?.Description ?? string.Empty
                        : description,
                    Icon = cls.Icon,
                    State = WorkspaceState.Active,
                    WorkspaceId = cls.WorkspaceId,
                    Workspace = null,
                    ClassId = cls.Id,
                    SecurityLevelId = CoreHub.SecurityLevelManager?.GetDefaultSecurityLevel(cls.Id)?.Id,
                    PortalRestricted = restricted,
                    CreatorId = me,
                    UpdaterId = me,
                    Created = now,
                    Updated = now
                };

                CoreHub.ObjectManager.Add(entity);

                // stamp the initial workflow status (the To-Do-category status of the class)
                var initialStatus = context.Statuses
                    .FirstOrDefault(s => MapCategory(context, s) == PortalIssueState.Open)
                    ?? context.Statuses.FirstOrDefault();

                if (context.WorkflowField is not null && initialStatus is not null)
                {
                    SetFieldValue(entity.Id, context.WorkflowField.Id, initialStatus.Name);
                }

                // stamp the requested priority (resolved against the class's priorities)
                var resolvedPriority = ResolvePriority(cls.Id, priority);

                if (context.PriorityField is not null && resolvedPriority is not null)
                {
                    SetFieldValue(entity.Id, context.PriorityField.Id, resolvedPriority.Name);
                }

                // the requester always watches their own submission
                if (me is Guid requesterId)
                {
                    CoreHub.WatcherManager.Add(entity.Id, requesterId);
                }

                var issue = BuildIssue(ReadObject(entity.Id) ?? entity, cls, viewer);

                IssueCreated?.Invoke(this, issue);

                return issue;
            }
        }

        /// <inheritdoc/>
        public IIssue AddComment(string issueKey, string text, string visibility)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            var viewer = Viewer.Resolve(null);
            var entity = ResolveIssueObject(issueKey, viewer, out var cls, out _);
            if (entity is null)
            {
                return null;
            }

            if (viewer.Id == Guid.Empty)
            {
                throw new PortalForbiddenException("A comment needs an author.");
            }

            // an unknown token widens rather than narrows: CommentVisibilityExtensions.Parse
            // reads anything it does not recognise as Public, so a client that misspells the
            // flag never gets a comment that is quietly hidden from the requester. The REST
            // endpoint rejects a malformed token before it reaches here.
            var audience = CommentVisibilityExtensions.Parse(visibility);

            // a customer cannot address the service team's notes - they would not see what
            // they wrote; the composer offers the internal audience to the service team only
            if (audience == CommentVisibility.InternalTeam && !CommentAudienceOf(entity, viewer).MayWriteInternal)
            {
                throw new PortalForbiddenException("Only the service team writes internal notes.");
            }

            var now = DateTime.UtcNow;

            var comment = new Comment
            {
                ObjectId = entity.Id,
                AuthorId = viewer.Id,
                Content = text,
                State = CommentState.Active,
                Visibility = audience,
                Created = now,
                Updated = now
            };

            CoreHub.CommentManager.Add(comment);
            Touch(entity, now, viewer);

            var issue = BuildIssue(entity, cls, viewer);
            var projected = issue.Comments.LastOrDefault(c => c.Id == comment.Id.ToString())
                ?? ToIssueComment(comment, issue.Requester);

            IssueCommented?.Invoke(this, projected);
            IssueUpdated?.Invoke(this, issue);

            return issue;
        }

        /// <inheritdoc/>
        public IIssue ShareIssue(string issueKey, IEnumerable<string> identityIds)
        {
            ArgumentNullException.ThrowIfNull(identityIds);

            var viewer = Viewer.Resolve(null);
            var entity = ResolveIssueObject(issueKey, viewer, out var cls, out var access);
            if (entity is null)
            {
                return null;
            }

            // sharing widens the audience of somebody's request, so it is the participants'
            // decision: the requester and those it was already shared with
            if (access is not (IssueAccess.Requester or IssueAccess.Participant or IssueAccess.System))
            {
                throw new PortalForbiddenException("Only the participants of an issue share it.");
            }

            var added = 0;

            // the share dialog offers the caller's organization, and what it did not offer is
            // not accepted from a hand-made request either (concept: a cross-tenant share is
            // refused); an identity outside it is skipped, not shared
            var organization = GetOrganizationMembers(viewer.Id)
                .Select(m => m.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in identityIds)
            {
                if (!Guid.TryParse(raw, out var identityId) || !organization.Contains(identityId.ToString()))
                {
                    continue;
                }

                var before = CoreHub.ShareManager.GetShares(entity.Id).Any(s => s.IdentityId == identityId);
                if (!before && CoreHub.ShareManager.Add(entity.Id, identityId) is not null)
                {
                    added++;
                }
            }

            if (added > 0)
            {
                Touch(entity, DateTime.UtcNow, viewer);
            }

            var issue = BuildIssue(entity, cls, viewer);

            if (added > 0)
            {
                IssueShared?.Invoke(this, issue);
                IssueUpdated?.Invoke(this, issue);
            }

            return issue;
        }

        /// <inheritdoc/>
        public IIssue UnshareIssue(string issueKey, string identityId)
        {
            var viewer = Viewer.Resolve(null);
            var entity = ResolveIssueObject(issueKey, viewer, out var cls, out var access);
            if (entity is null)
            {
                return null;
            }

            var parsed = Guid.TryParse(identityId, out var id) ? id : Guid.Empty;

            // the requester decides who shares their request; anyone it was shared with may
            // leave it, but not show somebody else the door
            var allowed = access is IssueAccess.Requester or IssueAccess.System
                || (access == IssueAccess.Participant && parsed == viewer.Id);

            if (!allowed)
            {
                throw new PortalForbiddenException("Only the requester revokes a share of somebody else.");
            }

            var removed = parsed != Guid.Empty && CoreHub.ShareManager.Remove(entity.Id, parsed);

            if (removed)
            {
                Touch(entity, DateTime.UtcNow, viewer);
            }

            // a participant who just left no longer sees the issue: they are answered with its
            // head data, which they saw a moment ago, and not with its timeline
            var issue = removed && parsed == viewer.Id && access == IssueAccess.Participant
                ? BuildIssue(entity, BuildClassContext(cls), BuildRequestType(cls), shares: null, watchers: null, comments: null, viewer: Viewer.System)
                : BuildIssue(entity, cls, viewer);

            if (removed)
            {
                IssueUnshared?.Invoke(this, issue);
                IssueUpdated?.Invoke(this, issue);
            }

            return issue;
        }

        /// <inheritdoc/>
        public IIssue SetRestricted(string issueKey, bool restricted)
        {
            var viewer = Viewer.Resolve(null);
            var entity = ResolveIssueObject(issueKey, viewer, out var cls, out var access);
            if (entity is null)
            {
                return null;
            }

            // the requester decides whether their organization sees their request; the service
            // team may change it on the operator's word when it may change the object at all
            var allowed = access is IssueAccess.Requester or IssueAccess.System
                || (access == IssueAccess.ServiceTeam
                    && CoreHub.PermissionManager?.IsGranted(viewer.Id, typeof(ObjectUpdatePermission), PageAuthorization.ChainOf(entity)) != false);

            if (!allowed)
            {
                throw new PortalForbiddenException("Only the requester changes who sees the issue.");
            }

            if (entity.PortalRestricted == restricted)
            {
                return BuildIssue(entity, cls, viewer);
            }

            entity.PortalRestricted = restricted;
            Touch(entity, DateTime.UtcNow, viewer);

            var issue = BuildIssue(entity, cls, viewer);

            IssueUpdated?.Invoke(this, issue);

            return issue;
        }

        /// <inheritdoc/>
        public IIssue Watch(string issueKey)
        {
            return SetWatching(issueKey, true);
        }

        /// <inheritdoc/>
        public IIssue Unwatch(string issueKey)
        {
            return SetWatching(issueKey, false);
        }

        /// <inheritdoc/>
        public IIssue AcceptResolution(string issueKey)
        {
            var viewer = Viewer.Resolve(null);
            var entity = ResolveIssueObject(issueKey, viewer, out var cls, out var access);
            if (entity is null)
            {
                return null;
            }

            RequireRequester(access);

            var context = BuildClassContext(cls);
            var current = BuildIssue(entity, cls, viewer);

            // closed issues are an idempotent no-op: re-accepting after the issue has
            // been closed is allowed (concept §API: "Accepting is idempotent on closed
            // issues — repeated calls have no further effect").
            if (current.PortalState == PortalIssueState.Closed)
            {
                return current;
            }

            // any other non-Resolved state is a 409 — the operator side never
            // proposed a resolution for the requester to confirm.
            if (current.PortalState != PortalIssueState.Resolved)
            {
                throw new PortalConflictException("No resolution has been proposed for this issue.");
            }

            // the closing status is the Done-category status whose name reads terminal
            // ("Closed", "Cancelled"); any other Done-category status is the fallback.
            var currentStatus = ResolveCurrentStatus(entity.Id, context);
            var target = context.Statuses.FirstOrDefault(s => IsTerminalName(s.Name))
                ?? context.Statuses.FirstOrDefault(s =>
                    MapCategoryName(context, s) == "done" && s.Id != currentStatus?.Id);

            if (context.WorkflowField is null || target is null)
            {
                return current;
            }

            var now = DateTime.UtcNow;
            SetFieldValue(entity.Id, context.WorkflowField.Id, target.Name);
            AddSystemComment(entity.Id, "Resolution accepted. The issue has been closed.", now, viewer);
            Touch(entity, now, viewer);

            var issue = BuildIssue(entity, cls, viewer);

            IssueResolutionAccepted?.Invoke(this, issue);
            IssueClosed?.Invoke(this, issue);
            IssueUpdated?.Invoke(this, issue);

            return issue;
        }

        /// <inheritdoc/>
        public IIssue RejectResolution(string issueKey, string reason)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);

            var viewer = Viewer.Resolve(null);
            var entity = ResolveIssueObject(issueKey, viewer, out var cls, out var access);
            if (entity is null)
            {
                return null;
            }

            RequireRequester(access);

            var context = BuildClassContext(cls);
            var current = BuildIssue(entity, cls, viewer);

            if (current.PortalState != PortalIssueState.Resolved)
            {
                throw new PortalConflictException("No resolution has been proposed for this issue.");
            }

            var target = context.Statuses.FirstOrDefault(s => MapCategoryName(context, s) == "inprogress");

            if (context.WorkflowField is null || target is null)
            {
                return current;
            }

            var now = DateTime.UtcNow;
            SetFieldValue(entity.Id, context.WorkflowField.Id, target.Name);
            AddSystemComment(entity.Id, $"Resolution rejected: {reason}", now, viewer);
            Touch(entity, now, viewer);

            var issue = BuildIssue(entity, cls, viewer);

            IssueResolutionRejected?.Invoke(this, issue);
            IssueUpdated?.Invoke(this, issue);

            return issue;
        }

        /// <summary>
        /// Release of unmanaged resources reserved during use.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public IReadOnlyList<Workspace> GetWorkspaces()
        {
            return GetWorkspaces(null);
        }

        /// <inheritdoc/>
        public IReadOnlyList<Workspace> GetWorkspaces(Guid? callerId)
        {
            var viewer = Viewer.Resolve(callerId);

            if (!viewer.IsSystem && viewer.Id == Guid.Empty)
            {
                return [];
            }

            var workspaces = ReadWorkspaces()
                .Where(w => w.State == WorkspaceState.Active);

            // tenant-scoped: an identity with a tenant only sees workspaces that
            // serve it. Operator-side identities (no tenant) see every active workspace
            // the operator side lets them read - useful for an internal "Settings" view
            // of the portal.
            if (viewer.TenantId is { } callerTenantId)
            {
                workspaces = workspaces
                    .Where(w => w.Tenants != null && w.Tenants.Any(t => t.Id == callerTenantId));
            }
            else if (!viewer.IsSystem)
            {
                workspaces = workspaces.Where(w => ContentVisibility.MayRead(w.Id));
            }

            return [.. workspaces.OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase)];
        }

        /// <inheritdoc/>
        public Workspace GetWorkspace(string workspaceKey)
        {
            if (string.IsNullOrWhiteSpace(workspaceKey))
            {
                return null;
            }

            return CoreHub.WorkspaceManager
                .GetWorkspaces(new Query<Workspace>())
                .FirstOrDefault(w => string.Equals(w.Key, workspaceKey, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public IReadOnlyList<Class> GetClasses(Guid workspaceId)
        {
            return [.. CoreHub.ClassManager
                .GetClasses(new Query<Class>().WhereEquals(x => x.WorkspaceId, workspaceId))
                .Where(c => c.State == ClassState.Active)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];
        }

        /// <inheritdoc/>
        public Class GetClass(Guid classId)
        {
            return CoreHub.ClassManager.GetClass(classId);
        }

        /// <inheritdoc/>
        public Class TogglePortalVisible(Guid classId)
        {
            var cls = CoreHub.ClassManager.GetClass(classId);
            if (cls is null)
            {
                return null;
            }

            cls.PortalVisible = !cls.PortalVisible;
            cls.Updated = DateTime.UtcNow;
            CoreHub.ClassManager.Update(cls);

            return cls;
        }

        /// <inheritdoc/>
        public IReadOnlyList<Field> GetFields(Guid classId)
        {
            return [.. CoreHub.FieldManager
                .GetFields(new ClassIdParameter(classId))
                .Where(f => !f.Deprecated && f.State == FieldState.Active)
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)];
        }

        /// <inheritdoc/>
        public IReadOnlyList<Status> GetStatuses(Guid classId)
        {
            return [.. CoreHub.StatusManager
                .GetStatuses(new ClassIdParameter(classId))
                .Where(s => s.State == StatusState.Active)
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
        }

        /// <inheritdoc/>
        public IReadOnlyList<Priority> GetPriorities(Guid classId)
        {
            return [.. CoreHub.PriorityManager
                .GetPriorities(new ClassIdParameter(classId))
                .Where(p => p.State == PriorityState.Active)
                .OrderBy(p => p.Order)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)];
        }

        /// <inheritdoc/>
        public IReadOnlyList<Form> GetForms(Guid classId)
        {
            return [.. CoreHub.FormManager
                .GetForms(new ClassIdParameter(classId))
                .Where(f => f.State == FormState.Active)
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)];
        }

        /// <inheritdoc/>
        public IReadOnlyList<Form> GetPortalTemplates(Guid classId)
        {
            return GetPortalTemplatesList(classId);
        }

        /// <inheritdoc/>
        public Form TogglePortalTemplate(Guid formId)
        {
            var form = CoreHub.FormManager
                .GetForms(new Query<Form>())
                .FirstOrDefault(f => f.Id == formId);
            if (form is null)
            {
                return null;
            }

            form.PortalTemplate = !form.PortalTemplate;
            form.Updated = DateTime.UtcNow;
            CoreHub.FormManager.Update(form);

            return form;
        }

        /// <inheritdoc/>
        public Field AddField(Guid classId, string name, string description, FieldType fieldType, FieldCardinality cardinality, bool required, bool uniqueConstraint)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            var cls = CoreHub.ClassManager.GetClass(classId)
                ?? throw new InvalidOperationException($"Class '{classId}' not found.");

            if (FieldManager.ReservedFieldNames.Any(r => string.Equals(r, name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Field name '{name}' is reserved.");
            }

            var now = DateTime.UtcNow;
            var field = new Field
            {
                Name = name,
                Description = description ?? string.Empty,
                ClassId = cls.Id,
                FieldType = fieldType,
                Cardinality = cardinality,
                Required = required,
                Unique = uniqueConstraint,
                Deprecated = false,
                State = FieldState.Active,
                Created = now,
                Updated = now
            };

            CoreHub.FieldManager.Add(field);
            return field;
        }

        /// <inheritdoc/>
        public Field UpdateField(Guid fieldId, string name, string description, FieldType fieldType, FieldCardinality cardinality, bool required, bool uniqueConstraint)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var field = CoreHub.FieldManager.GetField(fieldId);
            if (field is null)
            {
                return null;
            }

            field.Name = name;
            field.Description = description ?? string.Empty;
            field.FieldType = fieldType;
            field.Cardinality = cardinality;
            field.Required = required;
            field.Unique = uniqueConstraint;
            field.Updated = DateTime.UtcNow;

            CoreHub.FieldManager.Update(field);
            return field;
        }

        /// <inheritdoc/>
        public Field CloneField(Guid fieldId)
        {
            var source = CoreHub.FieldManager.GetField(fieldId);
            if (source is null)
            {
                return null;
            }

            var baseName = source.Name;
            var candidate = baseName + " (copy)";
            var suffix = 2;
            while (CoreHub.FieldManager.GetFields(new ClassIdParameter(source.ClassId)).Any(f => string.Equals(f.Name, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = $"{baseName} (copy {suffix++})";
            }

            return AddField(source.ClassId, candidate, source.Description, source.FieldType, source.Cardinality, source.Required, source.Unique);
        }

        /// <inheritdoc/>
        public bool DeleteField(Guid fieldId)
        {
            var field = CoreHub.FieldManager.GetField(fieldId);
            if (field is null)
            {
                return false;
            }

            field.Deprecated = true;
            field.Updated = DateTime.UtcNow;
            CoreHub.FieldManager.Update(field);

            return true;
        }

        /// <summary>
        /// Adds or removes the watch relationship between the current identity and the
        /// addressed issue and raises the corresponding events.
        /// </summary>
        /// <param name="issueKey">The issue key.</param>
        /// <param name="watch"><see langword="true"/> to watch, <see langword="false"/> to unwatch.</param>
        /// <returns>The updated issue projection, or <see langword="null"/> when the issue does not exist.</returns>
        /// <exception cref="PortalConflictException">
        /// Thrown when <paramref name="watch"/> is <see langword="false"/> and the calling
        /// identity is not subscribed to the issue (concept §API: 409 Conflict on
        /// <c>DELETE</c> of a non-existent subscription).
        /// </exception>
        private IIssue SetWatching(string issueKey, bool watch)
        {
            var viewer = Viewer.Resolve(null);
            var entity = ResolveIssueObject(issueKey, viewer, out var cls, out _);

            if (entity is null)
            {
                return null;
            }

            if (viewer.Id == Guid.Empty)
            {
                throw new PortalForbiddenException("Sign in to watch an issue.");
            }

            var me = viewer.Id;

            // the unwatch path is a state conflict when the caller has nothing to
            // unsubscribe from — the REST layer maps this to 409.
            if (!watch && !CoreHub.WatcherManager.GetWatchers(entity.Id).Any(w => w.IdentityId == me))
            {
                throw new PortalConflictException("You are not watching this issue.");
            }

            var changed = watch
                ? CoreHub.WatcherManager.Add(entity.Id, me) is not null
                : CoreHub.WatcherManager.Remove(entity.Id, me);

            if (changed)
            {
                Touch(entity, DateTime.UtcNow, viewer);
            }

            var issue = BuildIssue(entity, cls, viewer);

            if (changed)
            {
                if (watch) { IssueWatched?.Invoke(this, issue); }
                else { IssueUnwatched?.Invoke(this, issue); }
                IssueUpdated?.Invoke(this, issue);
            }

            return issue;
        }

        /// <summary>
        /// Refuses an action that belongs to the requester alone - accepting or rejecting the
        /// resolution of their request.
        /// </summary>
        /// <param name="access">The caller's access to the issue.</param>
        /// <exception cref="PortalForbiddenException">The caller is not the requester.</exception>
        private static void RequireRequester(IssueAccess access)
        {
            if (access is not (IssueAccess.Requester or IssueAccess.System))
            {
                throw new PortalForbiddenException("Only the requester accepts or rejects a resolution.");
            }
        }

        /// <summary>
        /// Fires the <see cref="IssueResolutionProposed"/> event for an issue whose
        /// workflow status has just been stamped with a Done-category, non-terminal
        /// status by the operator workflow. The portal REST layer never reaches this
        /// helper directly; it is intended to be called from the operator workflow
        /// code the moment a status transitions to the "awaiting requester
        /// confirmation" station of a portal-visible class.
        /// </summary>
        /// <param name="objectId">The underlying object id.</param>
        /// <returns>The projected issue when the event was raised, <see langword="null"/> otherwise.</returns>
        public IIssue NotifyResolutionProposed(Guid objectId)
        {
            // the event announces what happened to the issue, not what the operator who moved
            // it may read - the subscribers address the requester
            var entity = ReadObject(objectId);
            if (entity is null)
            {
                return null;
            }

            var cls = CoreHub.ClassManager.GetClass(entity.ClassId);
            if (cls is null || !cls.PortalVisible)
            {
                return null;
            }

            var context = BuildClassContext(cls);
            var issue = BuildIssue(entity, context, requestType: null, shares: null, watchers: null, comments: null, viewer: Viewer.System);

            if (issue.PortalState == PortalIssueState.Resolved)
            {
                IssueResolutionProposed?.Invoke(this, issue);
                return issue;
            }

            return null;
        }

        /// <inheritdoc/>
        public void Connect()
        {
            lock (_gate)
            {
                if (_connected)
                {
                    return;
                }

                _connected = true;
            }

            // every completed move is offered; NotifyResolutionProposed keeps the ones that
            // stamped a portal-visible object with a resolved state and drops the rest
            CoreHub.WorkflowManager.TransitionExecuted += (_, result) => OnTransitionExecuted(result);
        }

        /// <summary>
        /// Derives the portal's resolution event from a completed workflow transition.
        /// </summary>
        /// <remarks>
        /// The handler runs inside the operator's transition, so a failure here must not
        /// reach the workflow: the move was legitimate, and the portal not being told is the
        /// smaller fault. It is logged and the transition completes.
        /// </remarks>
        /// <param name="result">The completed transition.</param>
        private void OnTransitionExecuted(WorkflowTransitionResult result)
        {
            if (result?.Outcome != WorkflowTransitionOutcome.Executed)
            {
                return;
            }

            try
            {
                NotifyResolutionProposed(result.ObjectId);
            }
            catch (Exception ex)
            {
                _httpServerContext?.Log?.Exception(ex);
            }
        }

        /// <summary>
        /// Gets the identity acting on this call chain, or <see cref="Guid.Empty"/> for an
        /// anonymous caller and for the system.
        /// </summary>
        private static Guid Caller => CoreHub.SessionManager?.GetCurrentIdentityId(null) ?? Guid.Empty;

        /// <summary>
        /// Reads an object by id the way the portal's own rule needs it - without the
        /// operator-side narrowing, which <see cref="Viewer.AccessTo"/> replaces.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <returns>The object, or <see langword="null"/> when it does not exist.</returns>
        private static ObjectEntity ReadObject(Guid objectId)
        {
            using var unrestricted = CoreHub.SecurityLevelManager?.BeginUnrestricted();

            return CoreHub.ObjectManager.GetObject(objectId);
        }

        /// <summary>
        /// Reads objects without the operator-side narrowing, for the portal's own rule.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The materialized objects.</returns>
        private static List<ObjectEntity> ReadObjects(IQuery<ObjectEntity> query)
        {
            using var unrestricted = CoreHub.SecurityLevelManager?.BeginUnrestricted();

            return [.. CoreHub.ObjectManager.GetObjects(query)];
        }

        /// <summary>
        /// Reads the workspaces without the operator-side narrowing: which workspaces serve a
        /// tenant is a fact of the installation, not of the caller's grants.
        /// </summary>
        /// <returns>The workspaces.</returns>
        private static List<Workspace> ReadWorkspaces()
        {
            using var unrestricted = CoreHub.SecurityLevelManager?.BeginUnrestricted();

            return [.. CoreHub.WorkspaceManager.GetWorkspaces(new Query<Workspace>())];
        }

        /// <summary>
        /// Returns the active, non-abstract classes flagged portal-visible, ordered by
        /// name so the catalog (and the palette assignment) is stable.
        /// </summary>
        /// <returns>The portal-visible classes.</returns>
        private static List<Class> GetPortalClasses()
        {
            return [.. CoreHub.ClassManager
                .GetClasses(new Query<Class>().Where(x => x.PortalVisible))
                .Where(c => c.State == ClassState.Active && !c.IsAbstract)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];
        }

        /// <summary>
        /// Projects a portal-visible class to its request-type representation,
        /// including the portal-flagged forms as templates.
        /// </summary>
        /// <param name="cls">The class to project.</param>
        /// <returns>The request-type projection.</returns>
        private static RequestType BuildRequestType(Class cls)
        {
            var templates = GetPortalTemplatesList(cls.Id)
                .Select(f => new TemplateProjection
                {
                    Key = Slug(f.Name),
                    Title = f.Name,
                    Description = f.Description
                })
                .Cast<ITemplate>()
                .ToList();

            return new RequestType
            {
                Key = RequestTypeKey(cls),
                Title = cls.Name,
                Description = cls.Description,
                Icon = cls.Icon,
                Templates = templates
            };
        }

        /// <summary>
        /// Derives the stable key of the request type a class is offered as.
        /// </summary>
        /// <remarks>
        /// A class name is unique per workspace, not per installation - two service desks may
        /// both offer an <c>Incident</c> - so the key carries the workspace key in front of the
        /// class name: <c>sd-incident</c>. The workspace key is unique across the installation,
        /// which makes the pair unique; a class whose workspace is gone or carries no key falls
        /// back to the class name alone, so it can still be addressed.
        /// </remarks>
        /// <param name="cls">The portal-visible class.</param>
        /// <returns>The request-type key.</returns>
        internal static string RequestTypeKey(Class cls)
        {
            var workspaceKey = CoreHub.WorkspaceManager.GetWorkspace(cls.WorkspaceId)?.Key;
            var prefix = Slug(workspaceKey);

            return string.IsNullOrEmpty(prefix)
                ? Slug(cls.Name)
                : Slug(prefix + "-" + cls.Name);
        }

        /// <summary>
        /// Returns the active forms of the given class that are flagged as portal
        /// templates. Internal helper used by the request-type projection.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <returns>The portal-template forms.</returns>
        private static List<Form> GetPortalTemplatesList(Guid classId)
        {
            return [.. CoreHub.FormManager
                .GetForms(new ClassIdParameter(classId))
                .Where(f => f.PortalTemplate && f.State == FormState.Active)
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)];
        }

        /// <summary>
        /// Resolves the object behind an issue key, verifies that its class is portal-visible
        /// and that the viewer may see it.
        /// </summary>
        /// <remarks>
        /// An issue the viewer may not see is answered exactly like one that does not exist,
        /// so a guessed key discloses nothing.
        /// </remarks>
        /// <param name="issueKey">The issue key (e.g. <c>SD-17</c>).</param>
        /// <param name="viewer">The viewer.</param>
        /// <param name="cls">When the method returns, the portal-visible class of the object.</param>
        /// <param name="access">When the method returns, the viewer's access to the issue.</param>
        /// <returns>The object entity, or <see langword="null"/>.</returns>
        private static ObjectEntity ResolveIssueObject(string issueKey, Viewer viewer, out Class cls, out IssueAccess access)
        {
            cls = null;
            access = IssueAccess.None;

            if (string.IsNullOrWhiteSpace(issueKey))
            {
                return null;
            }

            ObjectEntity entity;

            using (CoreHub.SecurityLevelManager?.BeginUnrestricted())
            {
                entity = CoreHub.ObjectManager.GetObjectByKey(issueKey);
            }

            if (entity is null)
            {
                return null;
            }

            cls = CoreHub.ClassManager.GetClass(entity.ClassId);
            if (cls is null || !cls.PortalVisible)
            {
                cls = null;
                return null;
            }

            access = viewer.AccessTo(entity, CoreHub.ShareManager.GetShares(entity.Id).ToList(), operatorReadable: null);

            if (access == IssueAccess.None)
            {
                cls = null;
                return null;
            }

            return entity;
        }

        /// <summary>
        /// Captures the per-class lookup data needed by the issue projection — the
        /// workflow and priority fields, the class statuses, and the status categories.
        /// </summary>
        /// <param name="cls">The class to capture.</param>
        /// <returns>The class projection context.</returns>
        private static ClassContext BuildClassContext(Class cls)
        {
            var fields = CoreHub.FieldManager
                .GetFields(new ClassIdParameter(cls.Id))
                .Where(f => !f.Deprecated && f.State == FieldState.Active)
                .ToList();

            var statuses = CoreHub.StatusManager
                .GetStatuses(new ClassIdParameter(cls.Id))
                .Where(s => s.State == StatusState.Active)
                .ToList();

            var categories = CoreHub.StatusManager
                .GetStatusCategories(new Query<StatusCategory>())
                .ToDictionary(c => c.Id, c => c);

            return new ClassContext
            {
                Class = cls,
                WorkflowField = fields.FirstOrDefault(f => f.FieldType == FieldType.Workflow),
                PriorityField = fields.FirstOrDefault(f => f.FieldType == FieldType.Priority),
                Statuses = statuses,
                Categories = categories,
                Templates = GetPortalTemplatesList(cls.Id)
            };
        }

        /// <summary>
        /// Builds the full issue projection for a single object, loading the
        /// participants and the comment timeline.
        /// </summary>
        /// <param name="entity">The underlying object.</param>
        /// <param name="cls">The portal-visible class of the object.</param>
        /// <param name="viewer">The viewer the timeline and the relations are narrowed for.</param>
        /// <returns>The issue projection.</returns>
        private static Issue BuildIssue(ObjectEntity entity, Class cls, Viewer viewer)
        {
            var context = BuildClassContext(cls);
            var requestType = BuildRequestType(cls);
            var shares = CoreHub.ShareManager.GetShares(entity.Id).ToList();
            var watchers = CoreHub.WatcherManager.GetWatchers(entity.Id).ToList();
            var comments = CoreHub.CommentManager.GetComments(entity.Id).ToList();
            var relations = BuildRelations(entity, viewer);

            return BuildIssue(entity, context, requestType, shares, watchers, comments, viewer, relations);
        }

        /// <summary>
        /// Projects the relations of an object onto the links a portal reader is shown.
        /// </summary>
        /// <remarks>
        /// The portal is a constrained view, so a relation is projected only when its far
        /// end is itself a portal issue the viewer could open by key: an object of a
        /// portal-visible class that the portal's own rule lets the viewer see. A link to an
        /// internal record - a problem, a change, a bug in the development workspace - or to an
        /// external address stays on the operator side: its key, title and state are the
        /// service team's working material, not the requester's. Obsolete relations are
        /// history and are left out as well. The caption is read from this issue's side, the
        /// inverse label where it is the target.
        /// </remarks>
        /// <param name="entity">The object whose relations are projected.</param>
        /// <param name="viewer">The viewer.</param>
        /// <returns>The projected relations, ordered by caption and key.</returns>
        private static List<IssueRelation> BuildRelations(ObjectEntity entity, Viewer viewer)
        {
            var contexts = new Dictionary<Guid, ClassContext>();
            var relations = new List<IssueRelation>();

            foreach (var relation in CoreHub.ObjectRelationManager.GetRelations(entity.Id))
            {
                if (relation.Status == WebExpress.WebApp.WebRelation.RelationStatus.Obsolete
                    || relation.TargetObjectId is null)
                {
                    continue;
                }

                var outgoing = relation.SourceObjectId == entity.Id;
                var otherId = outgoing ? relation.TargetObjectId.Value : relation.SourceObjectId;

                if (otherId == entity.Id)
                {
                    continue;
                }

                var other = ReadObject(otherId);
                if (other is null)
                {
                    continue;
                }

                if (!contexts.TryGetValue(other.ClassId, out var context))
                {
                    var cls = CoreHub.ClassManager.GetClass(other.ClassId);
                    context = cls is not null && cls.PortalVisible ? BuildClassContext(cls) : null;
                    contexts[other.ClassId] = context;
                }

                // the far end exists for the viewer only when the portal would open it for them
                if (context is null
                    || viewer.AccessTo(other, CoreHub.ShareManager.GetShares(other.Id).ToList(), operatorReadable: null) == IssueAccess.None)
                {
                    continue;
                }

                var type = CoreHub.ObjectRelationTypeManager.GetRelationType(relation.TypeKey);
                var label = type is null
                    ? relation.TypeKey
                    : outgoing || type.Symmetric || string.IsNullOrWhiteSpace(type.InverseLabel)
                        ? type.Label
                        : type.InverseLabel;

                relations.Add(new IssueRelation
                {
                    Id = relation.Id.ToString(),
                    TypeKey = relation.TypeKey,
                    Label = string.IsNullOrWhiteSpace(label) ? relation.TypeKey : label,
                    IsOutgoing = outgoing,
                    Key = other.Key,
                    Title = other.Summary,
                    PortalState = ResolvePortalState(other.Id, context)
                });
            }

            return [.. relations
                .OrderBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Key, StringComparer.OrdinalIgnoreCase)];
        }

        /// <summary>
        /// Builds the issue projection from pre-loaded relations. Pass
        /// <see langword="null"/> for <paramref name="comments"/> to skip the timeline
        /// (used by list projections where only the head data is rendered).
        /// </summary>
        /// <param name="entity">The underlying object.</param>
        /// <param name="context">The class projection context.</param>
        /// <param name="requestType">The request-type projection of the object's class.</param>
        /// <param name="shares">The share relations of the object, or <see langword="null"/>.</param>
        /// <param name="watchers">The watch relations of the object, or <see langword="null"/>.</param>
        /// <param name="comments">The comments of the object, or <see langword="null"/> to omit.</param>
        /// <param name="viewer">The viewer the timeline is narrowed for.</param>
        /// <param name="relations">The projected relations, or <see langword="null"/> to omit.</param>
        /// <returns>The issue projection.</returns>
        private static Issue BuildIssue
        (
            ObjectEntity entity,
            ClassContext context,
            IRequestType requestType,
            IReadOnlyList<ObjectShare> shares,
            IReadOnlyList<ObjectWatcher> watchers,
            IReadOnlyList<Comment> comments,
            Viewer viewer,
            IReadOnlyList<IssueRelation> relations = null
        )
        {
            var requester = ToParticipant(ResolveIdentity(entity.CreatorId, entity.Creator));
            var sharedWith = (shares ?? [])
                .OrderBy(s => s.Created)
                .Select(s => ToParticipant(ResolveIdentity(s.IdentityId, s.Identity)))
                .Where(p => p is not null)
                .ToList();
            var watching = (watchers ?? [])
                .OrderBy(w => w.Created)
                .Select(w => ToParticipant(ResolveIdentity(w.IdentityId, w.Identity)))
                .Where(p => p is not null)
                .ToList();

            // the portal timeline is narrowed by the same audience rule as the operator
            // thread: the service team's notes reach the service team, never the customer
            var audience = comments is null ? default : CommentAudienceOf(entity, viewer);
            var timeline = comments is null
                ? []
                : comments
                    .Where(c => c.State != CommentState.Deleted)
                    .Where(c => CommentAudience.Reaches(c, audience.SeesInternal))
                    .OrderBy(c => c.Created)
                    .Select(c => ToIssueComment(c, requester))
                    .ToList();

            var assignee = ResolveIdentity(entity.AssigneeId, entity.Assignee);

            return new Issue
            {
                Key = entity.Key,
                Title = entity.Summary,
                Description = entity.Description,
                RequestType = requestType,
                RequestTypeName = context.Class.Name,
                Priority = ResolvePriorityCode(entity.Id, context),
                PortalState = ResolvePortalState(entity.Id, context),
                Requester = requester,
                AssigneeLabel = assignee?.Name ?? entity.Workspace?.Name ?? string.Empty,
                RequiresApproval = false,
                Restricted = entity.PortalRestricted,
                Created = entity.Created,
                Updated = entity.Updated,
                SharedWith = sharedWith,
                Watchers = watching,
                Comments = timeline,
                Relations = relations ?? []
            };
        }

        /// <summary>
        /// Answers what the viewer may do with the notes of an issue.
        /// </summary>
        /// <param name="entity">The issue's object.</param>
        /// <param name="viewer">The viewer.</param>
        /// <returns>Whether internal notes reach the viewer, and whether the viewer may write one.</returns>
        private static (bool SeesInternal, bool MayWriteInternal) CommentAudienceOf(ObjectEntity entity, Viewer viewer)
        {
            if (viewer.IsSystem)
            {
                return (true, true);
            }

            return (CommentAudience.SeesInternal(entity, viewer.Id), CommentAudience.MayWriteInternal(entity));
        }

        /// <summary>
        /// Resolves the collapsed portal state of an object from its workflow-field
        /// value. Objects without a stamped status read as <see cref="PortalIssueState.Open"/>;
        /// an unresolvable status value collapses to <see cref="PortalIssueState.InProgress"/>.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <param name="context">The class projection context.</param>
        /// <returns>The collapsed portal state.</returns>
        private static PortalIssueState ResolvePortalState(Guid objectId, ClassContext context)
        {
            if (context.WorkflowField is null)
            {
                return PortalIssueState.Open;
            }

            var data = CoreHub.ValueManager.GetValue(objectId, context.WorkflowField.Id)?.Data;
            if (string.IsNullOrWhiteSpace(data))
            {
                return PortalIssueState.Open;
            }

            var status = ResolveStatus(context, data);

            return status is null ? PortalIssueState.InProgress : MapCategory(context, status);
        }

        /// <summary>
        /// Returns the currently stamped status of the object, or <see langword="null"/>
        /// when the workflow field has no resolvable value.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <param name="context">The class projection context.</param>
        /// <returns>The current status, or <see langword="null"/>.</returns>
        private static Status ResolveCurrentStatus(Guid objectId, ClassContext context)
        {
            if (context.WorkflowField is null)
            {
                return null;
            }

            var data = CoreHub.ValueManager.GetValue(objectId, context.WorkflowField.Id)?.Data;

            return ResolveStatus(context, data);
        }

        /// <summary>
        /// Resolves a persisted workflow-field payload to a class status — first by
        /// normalized name, then by status id. Mirrors the operator-side resolution in
        /// <c>ObjectMetadataStatusFragment</c>.
        /// </summary>
        /// <param name="context">The class projection context.</param>
        /// <param name="data">The persisted value payload.</param>
        /// <returns>The matching status, or <see langword="null"/>.</returns>
        private static Status ResolveStatus(ClassContext context, string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            var normalized = Normalize(data);

            return context.Statuses.FirstOrDefault(s => Normalize(s.Name) == normalized)
                ?? context.Statuses.FirstOrDefault(s => string.Equals(s.Id.ToString(), data, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Maps a status to the collapsed portal state via its category. Inside the
        /// Done category, terminal names ("Closed", "Cancelled") read as
        /// <see cref="PortalIssueState.Closed"/>, everything else as
        /// <see cref="PortalIssueState.Resolved"/> (awaiting requester confirmation).
        /// Unknown categories collapse to <see cref="PortalIssueState.InProgress"/>.
        /// </summary>
        /// <param name="context">The class projection context.</param>
        /// <param name="status">The status to map.</param>
        /// <returns>The collapsed portal state.</returns>
        private static PortalIssueState MapCategory(ClassContext context, Status status)
        {
            return MapCategoryName(context, status) switch
            {
                "todo" => PortalIssueState.Open,
                "inprogress" => PortalIssueState.InProgress,
                "waiting" => PortalIssueState.WaitingOnRequester,
                "done" => IsTerminalName(status.Name) ? PortalIssueState.Closed : PortalIssueState.Resolved,
                _ => PortalIssueState.InProgress
            };
        }

        /// <summary>
        /// Returns the normalized category name of a status, or an empty string when
        /// the category cannot be resolved.
        /// </summary>
        /// <param name="context">The class projection context.</param>
        /// <param name="status">The status whose category is resolved.</param>
        /// <returns>The normalized category name.</returns>
        private static string MapCategoryName(ClassContext context, Status status)
        {
            return context.Categories.TryGetValue(status.CategoryId, out var category)
                ? Normalize(category.Name)
                : string.Empty;
        }

        /// <summary>
        /// Returns whether a status name marks a terminal lifecycle station.
        /// </summary>
        /// <param name="name">The status name.</param>
        /// <returns><see langword="true"/> for terminal names.</returns>
        private static bool IsTerminalName(string name)
        {
            var normalized = Normalize(name);

            return normalized is "closed" or "cancelled" or "canceled";
        }

        /// <summary>
        /// Resolves the priority code shown in the portal — the leading <c>P</c>-token
        /// of the stamped priority name (e.g. <c>P2</c> from <c>"P2 - High"</c>) or the
        /// full name when the class uses non-coded priorities.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <param name="context">The class projection context.</param>
        /// <returns>The priority display code, or an empty string when none is stamped.</returns>
        private static string ResolvePriorityCode(Guid objectId, ClassContext context)
        {
            if (context.PriorityField is null)
            {
                return string.Empty;
            }

            var data = CoreHub.ValueManager.GetValue(objectId, context.PriorityField.Id)?.Data;
            if (string.IsNullOrWhiteSpace(data))
            {
                return string.Empty;
            }

            return ShortPriority(data);
        }

        /// <summary>
        /// Shortens a priority name to its portal display code: a leading
        /// <c>P&lt;digit&gt;</c> token wins, otherwise the name is shown verbatim.
        /// </summary>
        /// <param name="name">The stamped priority name.</param>
        /// <returns>The display code.</returns>
        private static string ShortPriority(string name)
        {
            var trimmed = name.Trim();

            if (trimmed.Length >= 2
                && (trimmed[0] == 'P' || trimmed[0] == 'p')
                && char.IsDigit(trimmed[1])
                && (trimmed.Length == 2 || !char.IsLetterOrDigit(trimmed[2])))
            {
                return trimmed[..2].ToUpperInvariant();
            }

            return trimmed;
        }

        /// <summary>
        /// Resolves the requested priority code against the priorities defined for the
        /// class: an exact or prefix name match wins, then the positional index of
        /// <c>P&lt;n&gt;</c> codes, then the first (highest-ranked) priority.
        /// </summary>
        /// <param name="classId">The class id.</param>
        /// <param name="priority">The requested code (e.g. <c>P3</c>), or <see langword="null"/>.</param>
        /// <returns>The resolved priority, or <see langword="null"/> when the class has none.</returns>
        private static Priority ResolvePriority(Guid classId, string priority)
        {
            var priorities = CoreHub.PriorityManager
                .GetPriorities(new ClassIdParameter(classId))
                .Where(p => p.State == PriorityState.Active)
                .OrderBy(p => p.Order)
                .ToList();

            if (priorities.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                var normalized = Normalize(priority);

                var byName = priorities.FirstOrDefault(p => Normalize(p.Name) == normalized)
                    ?? priorities.FirstOrDefault(p => Normalize(p.Name).StartsWith(normalized, StringComparison.Ordinal));
                if (byName is not null)
                {
                    return byName;
                }

                if (priority.Length >= 2
                    && (priority[0] == 'P' || priority[0] == 'p')
                    && char.IsDigit(priority[1]))
                {
                    var index = priority[1] - '1';
                    if (index >= 0 && index < priorities.Count)
                    {
                        return priorities[index];
                    }
                }
            }

            // default to the mid-range priority so unprioritized submissions do not
            // page anyone (P3-equivalent for a four-step scale)
            return priorities[Math.Min(priorities.Count - 1, priorities.Count / 2)];
        }

        /// <summary>
        /// Inserts or updates the value row of (object, field).
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <param name="fieldId">The field id.</param>
        /// <param name="data">The payload to persist.</param>
        private static void SetFieldValue(Guid objectId, Guid fieldId, string data)
        {
            var existing = CoreHub.ValueManager.GetValue(objectId, fieldId);
            var now = DateTime.UtcNow;

            if (existing is null)
            {
                CoreHub.ValueManager.Add(new Value
                {
                    ObjectId = objectId,
                    FieldId = fieldId,
                    Data = data,
                    Created = now,
                    Updated = now
                });

                return;
            }

            existing.Data = data;
            existing.Updated = now;
            CoreHub.ValueManager.Update(existing);
        }

        /// <summary>
        /// Persists a machine-narration comment (status changes, resolution events)
        /// authored by the acting identity so the event is visible in the timeline.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <param name="text">The narration text.</param>
        /// <param name="timestamp">The timestamp of the event.</param>
        /// <param name="viewer">The acting viewer, whose name the narration carries.</param>
        private static void AddSystemComment(Guid objectId, string text, DateTime timestamp, Viewer viewer)
        {
            // a narration is still somebody's act; without anybody to name it is not written,
            // rather than signed with an account that did not act
            if (viewer.Id == Guid.Empty)
            {
                return;
            }

            CoreHub.CommentManager.Add(new Comment
            {
                ObjectId = objectId,
                AuthorId = viewer.Id,
                Content = text,
                State = CommentState.Active,

                // machine narration describes the lifecycle everyone with access already
                // sees, so it is never an internal note
                Visibility = CommentVisibility.Public,
                Created = timestamp,
                Updated = timestamp
            });
        }

        /// <summary>
        /// Bumps the object's update stamp and updater after a portal-side mutation.
        /// </summary>
        /// <param name="entity">The object to touch.</param>
        /// <param name="timestamp">The mutation timestamp.</param>
        /// <param name="viewer">The acting viewer.</param>
        private static void Touch(ObjectEntity entity, DateTime timestamp, Viewer viewer)
        {
            entity.Updated = timestamp;

            // never write the empty guid into an identity reference: the system acting leaves
            // the last updater in place
            if (viewer.Id != Guid.Empty)
            {
                entity.UpdaterId = viewer.Id;
            }

            using var unrestricted = CoreHub.SecurityLevelManager?.BeginUnrestricted();

            CoreHub.ObjectManager.Update(entity);
        }

        /// <summary>
        /// Resolves an identity from a pre-loaded navigation property or, failing that,
        /// from the identity manager.
        /// </summary>
        /// <param name="identityId">The identity id, or <see langword="null"/>.</param>
        /// <param name="loaded">The pre-loaded identity, or <see langword="null"/>.</param>
        /// <returns>The identity, or <see langword="null"/>.</returns>
        private static Identity ResolveIdentity(Guid? identityId, Identity loaded)
        {
            if (loaded is not null)
            {
                return loaded;
            }

            return identityId.HasValue ? CoreHub.IdentityManager.GetIdentity(identityId.Value) : null;
        }

        /// <summary>
        /// Projects an identity to the lightweight participant the portal renders.
        /// </summary>
        /// <param name="identity">The identity to project, or <see langword="null"/>.</param>
        /// <returns>The participant, or <see langword="null"/>.</returns>
        private static IssueParticipant ToParticipant(Identity identity)
        {
            if (identity is null)
            {
                return null;
            }

            return new IssueParticipant
            {
                Id = identity.Id.ToString(),
                Name = identity.Name,
                Email = identity.Email
            };
        }

        /// <summary>
        /// Projects a core comment to the portal timeline entry.
        /// </summary>
        /// <param name="comment">The comment to project.</param>
        /// <param name="requester">The issue requester, used to derive the role label.</param>
        /// <returns>The timeline entry.</returns>
        private static IssueComment ToIssueComment(Comment comment, IssueParticipant requester)
        {
            var author = ToParticipant(ResolveIdentity(comment.AuthorId, comment.Author));
            var isRequester = author is not null
                && requester is not null
                && string.Equals(author.Id, requester.Id, StringComparison.OrdinalIgnoreCase);

            return new IssueComment
            {
                Id = comment.Id.ToString(),
                Author = author,
                IsSystem = false,
                Role = isRequester ? "Requester" : "Service Team",
                Visibility = comment.Visibility.Token(),
                Timestamp = comment.Created,
                Text = comment.Content
            };
        }

        /// <summary>
        /// Builds the key of a new issue in a workspace that carries no key of its own, so the
        /// operator-side numbering has no prefix to count under.
        /// </summary>
        /// <param name="workspace">The workspace the issue is created in.</param>
        /// <returns>The allocated key.</returns>
        private static string NextFallbackKey(Workspace workspace)
        {
            var existing = ReadObjects(new Query<ObjectEntity>().WhereEquals(x => x.WorkspaceId, workspace?.Id ?? Guid.Empty))
                .Select(o => o.Key)
                .Where(k => !string.IsNullOrWhiteSpace(k) && k.StartsWith("ISSUE-", StringComparison.OrdinalIgnoreCase))
                .Select(k => int.TryParse(k["ISSUE-".Length..], out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"ISSUE-{existing + 1}";
        }

        /// <summary>
        /// Reduces a string to its lower-cased alphanumeric characters so loosely
        /// formatted slugs, names, and codes compare reliably.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <returns>The normalized string.</returns>
        private static string Normalize(string value)
        {
            return new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }

        /// <summary>
        /// Builds the URL-safe key of a display name: lower-cased, with every
        /// non-alphanumeric run collapsed to a single dash.
        /// </summary>
        /// <param name="value">The display name.</param>
        /// <returns>The slug.</returns>
        private static string Slug(string value)
        {
            var chars = new List<char>();
            var dash = false;

            foreach (var c in (value ?? string.Empty).Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    chars.Add(c);
                    dash = false;
                }
                else if (!dash && chars.Count > 0)
                {
                    chars.Add('-');
                    dash = true;
                }
            }

            while (chars.Count > 0 && chars[^1] == '-')
            {
                chars.RemoveAt(chars.Count - 1);
            }

            return new string([.. chars]);
        }

        /// <summary>
        /// What a viewer may do with an issue, by the role they have in it.
        /// </summary>
        private enum IssueAccess
        {
            /// <summary>The viewer does not see the issue.</summary>
            None,

            /// <summary>A member of the requester's organization; reads and comments.</summary>
            Organization,

            /// <summary>An identity the issue was shared with; reads, comments and shares.</summary>
            Participant,

            /// <summary>The requester; everything the portal offers.</summary>
            Requester,

            /// <summary>An operator-side account the operator side lets read the object.</summary>
            ServiceTeam,

            /// <summary>Nobody acting - start-up and background work - which sees everything.</summary>
            System
        }

        /// <summary>
        /// The caller a portal use case answers for, with what the access rule needs to know
        /// about them resolved once.
        /// </summary>
        private sealed class Viewer
        {
            /// <summary>
            /// The tenant of every requester asked about so far.
            /// </summary>
            private readonly Dictionary<Guid, Guid?> _tenants = [];

            /// <summary>
            /// The workspaces serving the viewer's tenant, resolved on first use.
            /// </summary>
            private HashSet<Guid> _served;

            /// <summary>
            /// Gets the viewer standing for the system: nobody acting.
            /// </summary>
            public static Viewer System { get; } = new() { IsSystem = true };

            /// <summary>
            /// Gets the identity of the viewer, <see cref="Guid.Empty"/> for an anonymous one.
            /// </summary>
            public Guid Id { get; private init; }

            /// <summary>
            /// Gets the tenant of the viewer, or <see langword="null"/> for an operator-side
            /// account.
            /// </summary>
            public Guid? TenantId { get; private init; }

            /// <summary>
            /// Gets a value indicating whether nobody acts and the system sees everything.
            /// </summary>
            public bool IsSystem { get; private init; }

            /// <summary>
            /// Gets a value indicating whether the viewer is a signed-in operator-side account.
            /// </summary>
            public bool IsOperator => !IsSystem && Id != Guid.Empty && TenantId is null;

            /// <summary>
            /// Resolves the viewer of a use case.
            /// </summary>
            /// <param name="callerId">The identity the caller named, or <see langword="null"/>
            /// for the identity acting on this call chain.</param>
            /// <returns>The viewer.</returns>
            public static Viewer Resolve(Guid? callerId)
            {
                if (callerId is null && CoreHub.SessionManager?.HasCaller != true)
                {
                    return System;
                }

                var id = callerId ?? Caller;
                var identity = id == Guid.Empty ? null : CoreHub.IdentityManager.GetIdentity(id);

                // an account that is locked or unknown is nobody: the session resolves such a
                // caller to the empty guid already, a caller-supplied id has to be checked here
                if (identity is null || identity.State != IdentityState.Active)
                {
                    return new Viewer();
                }

                return new Viewer { Id = id, TenantId = identity.TenantId };
            }

            /// <summary>
            /// Determines whether the viewer is offered the request types of a class: the
            /// class's workspace serves the viewer's tenant, or the viewer is an operator-side
            /// account. An anonymous viewer is offered nothing.
            /// </summary>
            /// <param name="cls">The portal-visible class.</param>
            /// <returns><see langword="true"/> when the class is offered.</returns>
            public bool Offers(Class cls)
            {
                if (IsSystem || IsOperator)
                {
                    return true;
                }

                return Id != Guid.Empty && Served().Contains(cls.WorkspaceId);
            }

            /// <summary>
            /// Answers the viewer's access to an issue.
            /// </summary>
            /// <param name="entity">The issue's object, read without the operator-side narrowing.</param>
            /// <param name="shares">The shares of the object, or <see langword="null"/>.</param>
            /// <param name="operatorReadable">The ids an operator-side viewer may read in the
            /// object's class, computed once for a list; <see langword="null"/> to ask the object
            /// manager for this one object.</param>
            /// <returns>The access.</returns>
            public IssueAccess AccessTo(ObjectEntity entity, IReadOnlyCollection<ObjectShare> shares, IReadOnlySet<Guid> operatorReadable)
            {
                if (IsSystem)
                {
                    return IssueAccess.System;
                }

                if (Id == Guid.Empty || entity is null)
                {
                    return IssueAccess.None;
                }

                if (entity.CreatorId == Id)
                {
                    return IssueAccess.Requester;
                }

                if (shares?.Any(s => s.IdentityId == Id) == true)
                {
                    return IssueAccess.Participant;
                }

                if (TenantId is Guid tenantId)
                {
                    var organization = !entity.PortalRestricted
                        && Served().Contains(entity.WorkspaceId)
                        && TenantOf(entity.CreatorId) == tenantId
                        && CoreHub.SecurityLevelManager?.IsCleared(Id, entity.SecurityLevelId) != false;

                    return organization ? IssueAccess.Organization : IssueAccess.None;
                }

                // an operator-side account sees what the operator side lets it read - its
                // workspace grants and its clearance
                var readable = operatorReadable is not null
                    ? operatorReadable.Contains(entity.Id)
                    : CoreHub.ObjectManager.GetObject(entity.Id) is not null;

                return readable ? IssueAccess.ServiceTeam : IssueAccess.None;
            }

            /// <summary>
            /// Returns the ids of the objects of a class the operator side lets the viewer read.
            /// </summary>
            /// <param name="classId">The class.</param>
            /// <returns>The readable ids.</returns>
            public IReadOnlySet<Guid> ReadableIds(Guid classId)
            {
                return CoreHub.ObjectManager
                    .GetObjects(new Query<ObjectEntity>().WhereEquals(x => x.ClassId, classId))
                    .Select(x => x.Id)
                    .ToHashSet();
            }

            /// <summary>
            /// Returns the tenant of a requester.
            /// </summary>
            /// <param name="identityId">The requester, or <see langword="null"/>.</param>
            /// <returns>The tenant, or <see langword="null"/>.</returns>
            public Guid? TenantOf(Guid? identityId)
            {
                if (identityId is not Guid id)
                {
                    return null;
                }

                if (!_tenants.TryGetValue(id, out var tenant))
                {
                    tenant = CoreHub.IdentityManager.GetIdentity(id)?.TenantId;
                    _tenants[id] = tenant;
                }

                return tenant;
            }

            /// <summary>
            /// Returns the workspaces serving the viewer's tenant.
            /// </summary>
            /// <returns>The workspace ids; empty for a viewer without a tenant.</returns>
            private HashSet<Guid> Served()
            {
                if (_served is not null)
                {
                    return _served;
                }

                _served = TenantId is Guid tenantId
                    ? [.. ReadWorkspaces()
                        .Where(w => w.Tenants != null && w.Tenants.Any(t => t.Id == tenantId))
                        .Select(w => w.Id)]
                    : [];

                return _served;
            }
        }

        /// <summary>
        /// Per-class lookup data shared by the projection helpers so each issue row
        /// does not re-query fields, statuses, and categories.
        /// </summary>
        private sealed class ClassContext
        {
            /// <summary>Gets or sets the projected class.</summary>
            public Class Class { get; init; }

            /// <summary>Gets or sets the workflow-typed field of the class, or <see langword="null"/>.</summary>
            public Field WorkflowField { get; init; }

            /// <summary>Gets or sets the priority-typed field of the class, or <see langword="null"/>.</summary>
            public Field PriorityField { get; init; }

            /// <summary>Gets or sets the active statuses of the class.</summary>
            public IReadOnlyList<Status> Statuses { get; init; }

            /// <summary>Gets or sets the status categories indexed by id.</summary>
            public IReadOnlyDictionary<Guid, StatusCategory> Categories { get; init; }

            /// <summary>Gets or sets the portal-template forms of the class.</summary>
            public IReadOnlyList<Form> Templates { get; init; }
        }
    }
}
