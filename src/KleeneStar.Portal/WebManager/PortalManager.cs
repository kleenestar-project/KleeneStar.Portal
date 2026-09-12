using KleeneStar.Core;
using KleeneStar.Core.WebManager;
using KleeneStar.Core.WebParameter;
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
    /// </remarks>
    public sealed class PortalManager : IPortalManager
    {
        private readonly IComponentHub _componentHub;
        private readonly IHttpServerContext _httpServerContext;

        /// <summary>
        /// Serializes issue creation so the per-workspace key allocation
        /// (<see cref="NextKey"/>) cannot hand out the same key twice.
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Whether <see cref="Connect"/> has subscribed to the operator-side events already.
        /// </summary>
        private bool _connected;

        /// <summary>
        /// Identity used as the acting portal user while the WebExpress identity flow
        /// does not yet expose the authenticated identity on the request. Mirrors the
        /// fallback of <c>KleeneStar.Core.WebManager.SessionManager</c> (the seeded
        /// admin identity) so portal actions are attributed to a valid row.
        /// </summary>
        public static readonly Guid FallbackIdentityId = Guid.Parse("77087646-B13A-44B1-9BAC-6E66443CEDFD");

        /// <summary>
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
        public IssueParticipant CurrentUser => ToParticipant(CoreHub.IdentityManager.GetIdentity(FallbackIdentityId));

        /// <inheritdoc/>
        public IReadOnlyList<IssueParticipant> GetOrganizationMembers()
        {
            return GetOrganizationMembers(FallbackIdentityId);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IssueParticipant> GetOrganizationMembers(Guid? callerId)
        {
            var me = callerId ?? FallbackIdentityId;
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
            var classes = GetPortalClasses();

            return [.. classes.Select(cls => BuildRequestType(cls))];
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
            return GetIssues(scope, FallbackIdentityId);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IIssue> GetIssues(IssueScope scope, Guid? callerId)
        {
            var classes = GetPortalClasses();
            var requestTypes = classes
                .Select(cls => (cls.Id, RequestType: BuildRequestType(cls)))
                .ToDictionary(x => x.Id, x => x.RequestType);

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

            var me = callerId ?? FallbackIdentityId;
            var meIdentity = CoreHub.IdentityManager.GetIdentity(me);

            // OrgScope tenant isolation (concept §Permissions): the caller sees
            // every issue of a portal-visible class whose workspace is shared
            // with the caller's tenant. Identities without a tenant (operator-side
            // accounts) see nothing in OrgScope. MineScope is unaffected.
            Guid? callerTenantId = meIdentity?.TenantId;
            HashSet<Guid> orgScopeWorkspaceIds = null;
            if (scope == IssueScope.Organization)
            {
                if (callerTenantId is null)
                {
                    return [];
                }

                orgScopeWorkspaceIds = CoreHub.WorkspaceManager
                    .GetWorkspaces(new Query<Workspace>())
                    .Where(w => w.Tenants != null && w.Tenants.Any(t => t.Id == callerTenantId.Value))
                    .Select(w => w.Id)
                    .ToHashSet();

                if (orgScopeWorkspaceIds.Count == 0)
                {
                    return [];
                }
            }

            var issues = new List<IIssue>();

            foreach (var cls in classes)
            {
                var context = BuildClassContext(cls);
                var objects = CoreHub.ObjectManager
                    .GetObjects(new Query<ObjectEntity>().WhereEquals(x => x.ClassId, cls.Id));

                foreach (var entity in objects)
                {
                    sharesByObject.TryGetValue(entity.Id, out var shares);
                    watchersByObject.TryGetValue(entity.Id, out var watchers);

                    if (scope == IssueScope.Mine)
                    {
                        var isMine = entity.CreatorId == me
                            || (shares?.Any(s => s.IdentityId == me) ?? false)
                            || (watchers?.Any(w => w.IdentityId == me) ?? false);

                        if (!isMine)
                        {
                            continue;
                        }
                    }
                    else if (scope == IssueScope.Organization)
                    {
                        // tenant isolation: drop issues whose workspace is not
                        // shared with the caller's tenant
                        if (!orgScopeWorkspaceIds!.Contains(entity.WorkspaceId))
                        {
                            continue;
                        }
                    }

                    issues.Add(BuildIssue(entity, context, requestTypes[cls.Id], shares, watchers, comments: null));
                }
            }

            return [.. issues.OrderByDescending(i => i.Updated)];
        }

        /// <inheritdoc/>
        public IIssue GetIssue(string issueKey)
        {
            var entity = ResolveIssueObject(issueKey, out var cls);

            return entity is null ? null : BuildIssue(entity, cls);
        }

        /// <inheritdoc/>
        public IIssue CreateIssue(string requestTypeKey, string templateKey, string title, string description, string priority)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            lock (_gate)
            {
                var cls = GetPortalClasses()
                    .FirstOrDefault(c => string.Equals(RequestTypeKey(c), Slug(requestTypeKey), StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Request type '{requestTypeKey}' not found.");

                var context = BuildClassContext(cls);

                // a template is a portal-flagged form of the class; it currently
                // contributes its description as a prefill when none was supplied.
                var template = string.IsNullOrWhiteSpace(templateKey)
                    ? null
                    : context.Templates.FirstOrDefault(f => string.Equals(Slug(f.Name), Slug(templateKey), StringComparison.OrdinalIgnoreCase));

                var workspace = CoreHub.WorkspaceManager.GetWorkspace(cls.WorkspaceId);
                var me = FallbackIdentityId;
                var now = DateTime.UtcNow;

                var entity = new ObjectEntity
                {
                    Key = NextKey(workspace),
                    Summary = title,
                    Description = string.IsNullOrWhiteSpace(description)
                        ? template?.Description ?? string.Empty
                        : description,
                    Icon = cls.Icon,
                    State = WorkspaceState.Active,
                    WorkspaceId = cls.WorkspaceId,
                    Workspace = null,
                    ClassId = cls.Id,
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
                CoreHub.WatcherManager.Add(entity.Id, me);

                var issue = BuildIssue(ReloadObject(entity.Id) ?? entity, cls);

                IssueCreated?.Invoke(this, issue);

                return issue;
            }
        }

        /// <inheritdoc/>
        public IIssue AddComment(string issueKey, string text, string visibility)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            var entity = ResolveIssueObject(issueKey, out var cls);
            if (entity is null)
            {
                return null;
            }

            var me = FallbackIdentityId;
            var now = DateTime.UtcNow;

            // an unknown token widens rather than narrows: CommentVisibilityExtensions.Parse
            // reads anything it does not recognise as Public, so a client that misspells the
            // flag never gets a comment that is quietly hidden from the requester. The REST
            // endpoint rejects a malformed token before it reaches here.
            var comment = new Comment
            {
                ObjectId = entity.Id,
                AuthorId = me,
                Content = text,
                State = CommentState.Active,
                Visibility = CommentVisibilityExtensions.Parse(visibility),
                Created = now,
                Updated = now
            };

            CoreHub.CommentManager.Add(comment);
            Touch(entity, now);

            var issue = BuildIssue(entity, cls);
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

            var entity = ResolveIssueObject(issueKey, out var cls);
            if (entity is null)
            {
                return null;
            }

            var added = 0;

            // the share dialog offers the caller's organization, and what it did not offer is
            // not accepted from a hand-made request either (concept: a cross-tenant share is
            // refused); an identity outside it is skipped, not shared
            var organization = GetOrganizationMembers()
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
                Touch(entity, DateTime.UtcNow);
            }

            var issue = BuildIssue(entity, cls);

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
            var entity = ResolveIssueObject(issueKey, out var cls);
            if (entity is null)
            {
                return null;
            }

            var removed = Guid.TryParse(identityId, out var parsed)
                && CoreHub.ShareManager.Remove(entity.Id, parsed);

            if (removed)
            {
                Touch(entity, DateTime.UtcNow);
            }

            var issue = BuildIssue(entity, cls);

            if (removed)
            {
                IssueUnshared?.Invoke(this, issue);
                IssueUpdated?.Invoke(this, issue);
            }

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
            var entity = ResolveIssueObject(issueKey, out var cls);
            if (entity is null)
            {
                return null;
            }

            var context = BuildClassContext(cls);
            var current = BuildIssue(entity, cls);

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
            AddSystemComment(entity.Id, "Resolution accepted. The issue has been closed.", now);
            Touch(entity, now);

            var issue = BuildIssue(entity, cls);

            IssueResolutionAccepted?.Invoke(this, issue);
            IssueClosed?.Invoke(this, issue);
            IssueUpdated?.Invoke(this, issue);

            return issue;
        }

        /// <inheritdoc/>
        public IIssue RejectResolution(string issueKey, string reason)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);

            var entity = ResolveIssueObject(issueKey, out var cls);
            if (entity is null)
            {
                return null;
            }

            var context = BuildClassContext(cls);
            var current = BuildIssue(entity, cls);

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
            AddSystemComment(entity.Id, $"Resolution rejected: {reason}", now);
            Touch(entity, now);

            var issue = BuildIssue(entity, cls);

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
            return GetWorkspaces(FallbackIdentityId);
        }

        /// <inheritdoc/>
        public IReadOnlyList<Workspace> GetWorkspaces(Guid? callerId)
        {
            var me = callerId ?? FallbackIdentityId;
            var meIdentity = CoreHub.IdentityManager.GetIdentity(me);

            var workspaces = CoreHub.WorkspaceManager
                .GetWorkspaces(new Query<Workspace>())
                .Where(w => w.State == WorkspaceState.Active);

            // tenant-scoped: an identity with a tenant only sees workspaces that
            // share one of its tenants. Operator-side identities (no tenant)
            // see every active workspace — useful for an internal "Settings"
            // view of the portal.
            if (meIdentity?.TenantId is { } callerTenantId)
            {
                workspaces = workspaces
                    .Where(w => w.Tenants != null && w.Tenants.Any(t => t.Id == callerTenantId));
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
            var entity = ResolveIssueObject(issueKey, out var cls);

            if (entity is null)
            {
                return null;
            }

            var me = FallbackIdentityId;

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
                Touch(entity, DateTime.UtcNow);
            }

            var issue = BuildIssue(entity, cls);

            if (changed)
            {
                if (watch) { IssueWatched?.Invoke(this, issue); }
                else { IssueUnwatched?.Invoke(this, issue); }
                IssueUpdated?.Invoke(this, issue);
            }

            return issue;
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
            var entity = CoreHub.ObjectManager.GetObject(objectId);
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
            var issue = BuildIssue(entity, context, requestType: null, shares: null, watchers: null, comments: null);

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
        /// Resolves the object behind an issue key and verifies that its class is
        /// portal-visible — objects of internal classes never surface as issues.
        /// </summary>
        /// <param name="issueKey">The issue key (e.g. <c>SD-17</c>).</param>
        /// <param name="cls">When the method returns, the portal-visible class of the object.</param>
        /// <returns>The object entity, or <see langword="null"/>.</returns>
        private static ObjectEntity ResolveIssueObject(string issueKey, out Class cls)
        {
            cls = null;

            if (string.IsNullOrWhiteSpace(issueKey))
            {
                return null;
            }

            var entity = CoreHub.ObjectManager.GetObjectByKey(issueKey);
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

            return entity;
        }

        /// <summary>
        /// Reloads an object after creation so navigation data populated by the data
        /// layer (e.g. the workspace) is available to the projection.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <returns>The reloaded object, or <see langword="null"/>.</returns>
        private static ObjectEntity ReloadObject(Guid objectId)
        {
            return CoreHub.ObjectManager.GetObject(objectId);
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
        /// <returns>The issue projection.</returns>
        private static Issue BuildIssue(ObjectEntity entity, Class cls)
        {
            var context = BuildClassContext(cls);
            var requestType = BuildRequestType(cls);
            var shares = CoreHub.ShareManager.GetShares(entity.Id).ToList();
            var watchers = CoreHub.WatcherManager.GetWatchers(entity.Id).ToList();
            var comments = CoreHub.CommentManager.GetComments(entity.Id).ToList();

            return BuildIssue(entity, context, requestType, shares, watchers, comments);
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
        /// <returns>The issue projection.</returns>
        private static Issue BuildIssue
        (
            ObjectEntity entity,
            ClassContext context,
            IRequestType requestType,
            IReadOnlyList<ObjectShare> shares,
            IReadOnlyList<ObjectWatcher> watchers,
            IReadOnlyList<Comment> comments
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

            // the portal timeline is visibility-filtered (concept §Portal Manager); the
            // operator-side thread is not, so the filter belongs here rather than in the
            // comment manager
            var timeline = comments is null
                ? []
                : comments
                    .Where(c => c.State != CommentState.Deleted)
                    .Where(c => IsVisibleToViewer(c, entity, FallbackIdentityId))
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
                Created = entity.Created,
                Updated = entity.Updated,
                SharedWith = sharedWith,
                Watchers = watching,
                Comments = timeline
            };
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
        private static void AddSystemComment(Guid objectId, string text, DateTime timestamp)
        {
            CoreHub.CommentManager.Add(new Comment
            {
                ObjectId = objectId,
                AuthorId = FallbackIdentityId,
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
        private static void Touch(ObjectEntity entity, DateTime timestamp)
        {
            entity.Updated = timestamp;
            entity.UpdaterId = FallbackIdentityId;
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
        /// Determines whether a comment reaches a portal viewer.
        /// </summary>
        /// <remarks>
        /// A <see cref="CommentVisibility.Public"/> comment reaches everyone who can open
        /// the issue. A <see cref="CommentVisibility.InternalTeam"/> comment reaches the
        /// assigned service group plus the requester (concept §Issue Detail): the object's
        /// creator, its assignee, and the comment's own author. Identities the issue was
        /// merely shared with, and watchers, do not see it.
        /// <para>
        /// The viewer is <see cref="FallbackIdentityId"/> until the WebExpress identity flow
        /// exposes the authenticated identity on the request — the same stand-in every other
        /// portal action uses.
        /// </para>
        /// </remarks>
        /// <param name="comment">The comment to test.</param>
        /// <param name="entity">The object the comment hangs on.</param>
        /// <param name="viewerId">The identity reading the timeline.</param>
        /// <returns><see langword="true"/> when the comment reaches the viewer.</returns>
        private static bool IsVisibleToViewer(Comment comment, ObjectEntity entity, Guid viewerId)
        {
            if (comment.Visibility != CommentVisibility.InternalTeam)
            {
                return true;
            }

            return entity.CreatorId == viewerId
                || entity.AssigneeId == viewerId
                || comment.AuthorId == viewerId;
        }

        /// <summary>
        /// Allocates the next issue key inside the workspace by incrementing the highest
        /// numeric key suffix (<c>SD-17</c> → <c>SD-18</c>). Callers must hold
        /// <see cref="_gate"/>.
        /// </summary>
        /// <param name="workspace">The workspace the issue is created in.</param>
        /// <returns>The allocated key.</returns>
        private static string NextKey(Workspace workspace)
        {
            var prefix = string.IsNullOrWhiteSpace(workspace?.Key) ? "ISSUE" : workspace.Key;

            var existing = CoreHub.ObjectManager
                .GetObjects(new Query<ObjectEntity>().WhereEquals(x => x.WorkspaceId, workspace?.Id ?? Guid.Empty))
                .Select(o => o.Key)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k =>
                {
                    var separator = k.LastIndexOf('-');
                    return separator >= 0 && int.TryParse(k[(separator + 1)..], out var n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"{prefix}-{existing + 1}";
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
