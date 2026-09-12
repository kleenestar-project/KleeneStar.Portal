using KleeneStar.Core;
using KleeneStar.Model.Entities;
using KleeneStar.Portal.WebDomain;
using KleeneStar.Portal.WebManager;
using ObjectEntity = KleeneStar.Model.Entities.Object;

namespace KleeneStar.Portal.Test.WebManager
{
    /// <summary>
    /// Provides unit tests for <see cref="PortalManager"/> — the composition of the
    /// operator-side managers into the portal use cases: request-type catalog, issue
    /// listing and projection, creation, commenting, sharing, watching, and the
    /// resolution confirmation flow.
    /// </summary>
    [Collection("NonParallelTests")]
    public class UnitTestPortalManager
    {
        private static readonly Guid WorkspaceId = Guid.Parse("11A1B1C1-D1E1-4F11-8111-A11111111111");
        private static readonly Guid IncidentClassId = Guid.Parse("22A2B2C2-D2E2-4F22-8222-A22222222222");
        private static readonly Guid InternalClassId = Guid.Parse("33A3B3C3-D3E3-4F33-8333-A33333333333");
        private static readonly Guid WorkflowFieldId = Guid.Parse("44A4B4C4-D4E4-4F44-8444-A44444444444");
        private static readonly Guid PriorityFieldId = Guid.Parse("55A5B5C5-D5E5-4F55-8555-A55555555555");
        private static readonly Guid MemberIdentityId = Guid.Parse("66A6B6C6-D6E6-4F66-8666-A66666666666");
        private static readonly Guid MineObjectId = Guid.Parse("77A7B7C7-D7E7-4F77-8777-A77777777777");
        private static readonly Guid ForeignObjectId = Guid.Parse("88A8B8C8-D8E8-4F88-8888-A88888888888");
        private static readonly Guid InternalObjectId = Guid.Parse("99A9B9C9-D9E9-4F99-8999-A99999999999");

        private static readonly Guid CategoryTodoId = Guid.Parse("AA0A0A0A-0A0A-4AAA-8AAA-AAAAAAAAAAAA");
        private static readonly Guid CategoryInProgressId = Guid.Parse("BB0B0B0B-0B0B-4BBB-8BBB-BBBBBBBBBBBB");
        private static readonly Guid CategoryWaitingId = Guid.Parse("CC0C0C0C-0C0C-4CCC-8CCC-CCCCCCCCCCCC");
        private static readonly Guid CategoryDoneId = Guid.Parse("DD0D0D0D-0D0D-4DDD-8DDD-DDDDDDDDDDDD");

        // Tenant isolation test fixtures.
        private static readonly Guid AcmeTenantId = Guid.Parse("EEEEEEEE-EEEE-4EEE-8EEE-EEEEEEEEEEEE");
        private static readonly Guid GlobexTenantId = Guid.Parse("FFFFFFFF-FFFF-4FFF-8FFF-FFFFFFFFFFFF");
        private static readonly Guid GlobexWorkspaceId = Guid.Parse("0A0A0A0A-0A0A-4A0A-8A0A-0A0A0A0A0A0A");
        private static readonly Guid GlobexMemberIdentityId = Guid.Parse("0B0B0B0B-0B0B-4B0B-8B0B-0B0B0B0B0B0B");
        private static readonly Guid GlobexObjectId = Guid.Parse("0C0C0C0C-0C0C-4C0C-8C0C-0C0C0C0C0C0C");
        private static readonly Guid TenantlessIdentityId = Guid.Parse("0D0D0D0D-0D0D-4D0D-8D0D-0D0D0D0D0D0D");
        private static readonly Guid GlobexIncidentClassId = Guid.Parse("CCCCCCCC-CCCC-4CCC-8CCC-CCCCCCCCCCCC");

        /// <summary>
        /// Seeds the in-memory database with the full portal world: a workspace, a
        /// portal-visible Incident class (with workflow/priority fields, statuses across
        /// all four categories, P1–P4 priorities, and a portal-template form), an
        /// internal class that must never surface, the acting admin identity (the
        /// portal's fallback identity), a second tenant member, and two issues.
        /// </summary>
        /// <param name="connectionString">The per-test in-memory database name.</param>
        /// <returns>The portal manager under test.</returns>
        private static PortalManager Seed(string connectionString)
        {
            var manager = PortalHubFixture.Initialize(connectionString);

            using var db = PortalHubFixture.CreateDbContext(connectionString);

            if (db.Workspaces.Any(x => x.Id == WorkspaceId))
            {
                return manager;
            }

            // tenant isolation fixtures — the standard workspace belongs to Acme;
            // a sibling workspace for Globex shares the portal-visible class.
            // Seeding them here keeps every existing test compatible while the
            // new GetIssues_OrganizationScope_* cases drive the cross-tenant
            // isolation behaviour.
            var acmeTenant = new Tenant(AcmeTenantId) { Name = "Acme Corp", Description = "Acme tenant." };
            var globexTenant = new Tenant(GlobexTenantId) { Name = "Globex Inc", Description = "Globex tenant." };
            db.Tenants.Add(acmeTenant);
            db.Tenants.Add(globexTenant);

            db.Workspaces.Add(new Workspace
            {
                Id = WorkspaceId,
                Key = "SD",
                Name = "IT Service Desk",
                Tenants = [acmeTenant]
            });

            db.Workspaces.Add(new Workspace
            {
                Id = GlobexWorkspaceId,
                Key = "SDG",
                Name = "Globex Service Desk",
                Tenants = [globexTenant]
            });

            db.Classes.Add(new Class
            {
                Id = IncidentClassId,
                Name = "Incident",
                Description = "Incident management.",
                WorkspaceId = WorkspaceId,
                State = ClassState.Active,
                PortalVisible = true
            });
            // the same class is portal-visible in the Globex workspace so
            // GetIssues(Organization) finds tenant-eligible issues there too.
            db.Classes.Add(new Class
            {
                Id = GlobexIncidentClassId,
                Name = "Incident",
                Description = "Incident management (Globex).",
                WorkspaceId = GlobexWorkspaceId,
                State = ClassState.Active,
                PortalVisible = true
            });
            db.Classes.Add(new Class
            {
                Id = InternalClassId,
                Name = "Problem",
                Description = "Internal problem management.",
                WorkspaceId = WorkspaceId,
                State = ClassState.Active,
                PortalVisible = false
            });

            db.StatusCategories.Add(new StatusCategory(CategoryTodoId) { Name = "ToDo" });
            db.StatusCategories.Add(new StatusCategory(CategoryInProgressId) { Name = "InProgress" });
            db.StatusCategories.Add(new StatusCategory(CategoryWaitingId) { Name = "Waiting" });
            db.StatusCategories.Add(new StatusCategory(CategoryDoneId) { Name = "Done" });

            void addStatus(string name, Guid categoryId) => db.Statuses.Add(new Status
            {
                Id = Guid.NewGuid(),
                Name = name,
                State = StatusState.Active,
                CategoryId = categoryId,
                ClassId = IncidentClassId,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });

            addStatus("New", CategoryTodoId);
            addStatus("In Progress", CategoryInProgressId);
            addStatus("Waiting on Requester", CategoryWaitingId);
            addStatus("Resolved", CategoryDoneId);
            addStatus("Closed", CategoryDoneId);

            db.Fields.Add(new Field
            {
                Id = WorkflowFieldId,
                Name = "Status",
                ClassId = IncidentClassId,
                FieldType = FieldType.Workflow,
                State = FieldState.Active,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
            db.Fields.Add(new Field
            {
                Id = PriorityFieldId,
                Name = "Priority",
                ClassId = IncidentClassId,
                FieldType = FieldType.Priority,
                State = FieldState.Active,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });

            var order = 0;
            foreach (var name in new[] { "P1 - Critical", "P2 - High", "P3 - Moderate", "P4 - Low" })
            {
                db.Priorities.Add(new Priority
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    ClassId = IncidentClassId,
                    State = PriorityState.Active,
                    Order = order++,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                });
            }

            db.Forms.Add(new Form
            {
                Id = Guid.NewGuid(),
                Name = "Self-Service Form",
                Description = "Simplified form for end users.",
                ClassId = IncidentClassId,
                FormType = FormType.Default,
                State = FormState.Active,
                PortalTemplate = true,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
            db.Forms.Add(new Form
            {
                Id = Guid.NewGuid(),
                Name = "Resolver Form",
                Description = "Detailed form for support agents.",
                ClassId = IncidentClassId,
                FormType = FormType.Default,
                State = FormState.Active,
                PortalTemplate = false,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });

            db.Identities.Add(new Identity(PortalManager.FallbackIdentityId)
            {
                Name = "Admin User",
                Email = "admin@kleenestar.org",
                PasswordHash = "$test$",
                TenantId = AcmeTenantId,
                Tenant = acmeTenant
            });
            db.Identities.Add(new Identity(MemberIdentityId)
            {
                Name = "Anna Becker",
                Email = "anna.becker@kleenestar.org",
                PasswordHash = "$test$",
                TenantId = AcmeTenantId,
                Tenant = acmeTenant
            });
            // second tenant member — exercises the cross-tenant isolation path.
            db.Identities.Add(new Identity(GlobexMemberIdentityId)
            {
                Name = "Gina Globex",
                Email = "gina.globex@globex.example",
                PasswordHash = "$test$",
                TenantId = GlobexTenantId,
                Tenant = globexTenant
            });
            // an operator-style identity without a tenant — must be excluded
            // from IssueScope.Organization entirely.
            db.Identities.Add(new Identity(TenantlessIdentityId)
            {
                Name = "Operator Sam",
                Email = "sam@kleenestar.org",
                PasswordHash = "$test$"
            });

            db.Objects.Add(new ObjectEntity(MineObjectId)
            {
                Key = "SD-1",
                Summary = "Outlook not receiving external mail",
                Description = "No external mail since this morning.",
                WorkspaceId = WorkspaceId,
                ClassId = IncidentClassId,
                CreatorId = PortalManager.FallbackIdentityId,
                Created = DateTime.UtcNow.AddDays(-2),
                Updated = DateTime.UtcNow.AddHours(-2)
            });
            db.Objects.Add(new ObjectEntity(ForeignObjectId)
            {
                Key = "SD-2",
                Summary = "Printer prints stripes",
                Description = "Vertical black stripes on every page.",
                WorkspaceId = WorkspaceId,
                ClassId = IncidentClassId,
                CreatorId = MemberIdentityId,
                Created = DateTime.UtcNow.AddDays(-1),
                Updated = DateTime.UtcNow.AddHours(-1)
            });
            db.Objects.Add(new ObjectEntity(InternalObjectId)
            {
                Key = "SD-3",
                Summary = "Internal problem record",
                WorkspaceId = WorkspaceId,
                ClassId = InternalClassId,
                CreatorId = PortalManager.FallbackIdentityId,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
            // a Globex-side issue — Globex is the only tenant on GlobexWorkspaceId
            // so IssueScope.Organization must show it to GlobexMember and hide
            // it from any Acme / tenant-less identity.
            var globexIncidentClassId = Guid.Parse("CCCCCCCC-CCCC-4CCC-8CCC-CCCCCCCCCCCC");
            db.Objects.Add(new ObjectEntity(GlobexObjectId)
            {
                Key = "SDG-1",
                Summary = "Globex VPN drops",
                Description = "Connection drops every ten minutes.",
                WorkspaceId = GlobexWorkspaceId,
                ClassId = globexIncidentClassId,
                CreatorId = GlobexMemberIdentityId,
                Created = DateTime.UtcNow.AddDays(-1),
                Updated = DateTime.UtcNow.AddHours(-3)
            });

            void addValue(Guid objectId, Guid fieldId, string data) => db.Values.Add(new Value
            {
                Id = Guid.NewGuid(),
                ObjectId = objectId,
                FieldId = fieldId,
                Data = data,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });

            addValue(MineObjectId, WorkflowFieldId, "In Progress");
            addValue(MineObjectId, PriorityFieldId, "P2 - High");
            addValue(ForeignObjectId, WorkflowFieldId, "New");
            addValue(ForeignObjectId, PriorityFieldId, "P3 - Moderate");

            db.SaveChanges();

            return manager;
        }

        /// <summary>
        /// Stamps the workflow value of an object directly in the database — used to
        /// drive the state-projection cases without going through the manager.
        /// </summary>
        /// <param name="connectionString">The per-test in-memory database name.</param>
        /// <param name="objectId">The object whose status is stamped.</param>
        /// <param name="statusName">The status name to persist.</param>
        private static void StampStatus(string connectionString, Guid objectId, string statusName)
        {
            using var db = PortalHubFixture.CreateDbContext(connectionString);
            var value = db.Values.First(v => v.ObjectId == objectId && v.FieldId == WorkflowFieldId);
            value.Data = statusName;
            db.SaveChanges();
        }

        /// <summary>
        /// The catalog contains only portal-visible classes, and templates only the
        /// portal-flagged forms of the class.
        /// </summary>
        [Fact]
        public void GetRequestTypes_ReturnsOnlyPortalVisibleClasses()
        {
            var manager = Seed(nameof(GetRequestTypes_ReturnsOnlyPortalVisibleClasses));

            var requestTypes = manager.GetRequestTypes();

            // the seed provisions two portal-visible Incident classes (Acme + Globex)
            // sharing the same name; both are exposed as request types.
            Assert.Equal(2, requestTypes.Count);
            var acme = requestTypes.Single(rt => rt.Description == "Incident management.");
            Assert.Equal("sd-incident", acme.Key);
            Assert.Equal("Incident", acme.Title);

            var template = Assert.Single(acme.Templates);
            Assert.Equal("self-service-form", template.Key);
            Assert.Equal("Self-Service Form", template.Title);
        }

        /// <summary>
        /// A request type can be resolved by its slug key, case-insensitively.
        /// </summary>
        [Fact]
        public void GetRequestType_ByKey_IsCaseInsensitive()
        {
            var manager = Seed(nameof(GetRequestType_ByKey_IsCaseInsensitive));

            Assert.NotNull(manager.GetRequestType("SD-Incident"));
            Assert.NotNull(manager.GetRequestType("SD-INCIDENT"));
            Assert.Null(manager.GetRequestType("sd-problem"));
        }

        /// <summary>
        /// The organization scope lists every issue of every portal-visible class
        /// belonging to a workspace the caller's tenant is shared with, and nothing
        /// from internal classes or from workspaces the caller is not part of.
        /// </summary>
        [Fact]
        public void GetIssues_OrganizationScope_ExcludesInternalClasses()
        {
            var manager = Seed(nameof(GetIssues_OrganizationScope_ExcludesInternalClasses));

            // the seeded admin is an Acme member; both SD-1 and SD-2 live in the
            // Acme workspace, so the Acme view sees both. SD-3 is on the internal
            // class and must be excluded; the Globex issue (SDG-1) is on a
            // workspace Acme is not part of, so it must be excluded too.
            var issues = manager.GetIssues(IssueScope.Organization);

            Assert.Equal(2, issues.Count);
            Assert.DoesNotContain(issues, i => i.Key == "SD-3");
            Assert.DoesNotContain(issues, i => i.Key == "SDG-1");
        }

        /// <summary>
        /// IssueScope.Organization is tenant-scoped: a Globex member sees only the
        /// Globex workspace's issues, never the Acme workspace's.
        /// </summary>
        [Fact]
        public void GetIssues_OrganizationScope_IsTenantIsolated()
        {
            var manager = Seed(nameof(GetIssues_OrganizationScope_IsTenantIsolated));

            var acmeView = manager.GetIssues(IssueScope.Organization, PortalManager.FallbackIdentityId);
            var globexView = manager.GetIssues(IssueScope.Organization, GlobexMemberIdentityId);

            // the seeded admin (Acme) sees every issue of the Acme workspace
            // (SD-1 and SD-2) but never the Globex workspace's issue.
            Assert.Equal(2, acmeView.Count);
            Assert.DoesNotContain(acmeView, i => i.Key == "SDG-1");

            // the Globex member only sees the Globex workspace's issue.
            Assert.Single(globexView);
            Assert.Equal("SDG-1", globexView[0].Key);
        }

        /// <summary>
        /// Operator-side identities (no tenant) are excluded from
        /// <see cref="IssueScope.Organization"/> entirely.
        /// </summary>
        [Fact]
        public void GetIssues_OrganizationScope_ExcludesTenantlessIdentities()
        {
            var manager = Seed(nameof(GetIssues_OrganizationScope_ExcludesTenantlessIdentities));

            var org = manager.GetIssues(IssueScope.Organization, TenantlessIdentityId);

            Assert.Empty(org);
        }

        /// <summary>
        /// The mine scope keeps only issues the current identity requested, was
        /// shared on, or watches.
        /// </summary>
        [Fact]
        public void GetIssues_MineScope_FiltersByParticipation()
        {
            var manager = Seed(nameof(GetIssues_MineScope_FiltersByParticipation));

            var mine = manager.GetIssues(IssueScope.Mine);
            var issue = Assert.Single(mine);
            Assert.Equal("SD-1", issue.Key);

            // sharing the foreign issue with the current identity pulls it into scope
            manager.ShareIssue("SD-2", [PortalManager.FallbackIdentityId.ToString()]);

            mine = manager.GetIssues(IssueScope.Mine);
            Assert.Equal(2, mine.Count);
        }

        /// <summary>
        /// The single-issue projection carries the head data, the collapsed state, the
        /// shortened priority code, and the requester resolved from the creator.
        /// </summary>
        [Fact]
        public void GetIssue_ProjectsHeadStateAndPriority()
        {
            var manager = Seed(nameof(GetIssue_ProjectsHeadStateAndPriority));

            var issue = manager.GetIssue("SD-1");

            Assert.NotNull(issue);
            Assert.Equal("Outlook not receiving external mail", issue.Title);
            Assert.Equal(PortalIssueState.InProgress, issue.PortalState);
            Assert.Equal("P2", issue.Priority);
            Assert.Equal("Incident", issue.RequestTypeName);
            Assert.NotNull(issue.Requester);
            Assert.Equal("Admin User", issue.Requester.Name);
        }

        /// <summary>
        /// Issues of internal (non-portal) classes never surface, even when addressed
        /// directly by key.
        /// </summary>
        [Fact]
        public void GetIssue_InternalClass_ReturnsNull()
        {
            var manager = Seed(nameof(GetIssue_InternalClass_ReturnsNull));

            Assert.Null(manager.GetIssue("SD-3"));
        }

        /// <summary>
        /// The category of the stamped status drives the collapsed portal state:
        /// To-Do reads Open, Waiting reads WaitingOnRequester, the Done category splits
        /// into Resolved (non-terminal name) and Closed (terminal name).
        /// </summary>
        [Theory]
        [InlineData("New", PortalIssueState.Open)]
        [InlineData("In Progress", PortalIssueState.InProgress)]
        [InlineData("Waiting on Requester", PortalIssueState.WaitingOnRequester)]
        [InlineData("Resolved", PortalIssueState.Resolved)]
        [InlineData("Closed", PortalIssueState.Closed)]
        public void GetIssue_MapsStatusCategoryToPortalState(string statusName, PortalIssueState expected)
        {
            var connectionString = nameof(GetIssue_MapsStatusCategoryToPortalState) + statusName.Replace(" ", string.Empty);
            var manager = Seed(connectionString);

            StampStatus(connectionString, MineObjectId, statusName);

            Assert.Equal(expected, manager.GetIssue("SD-1")!.PortalState);
        }

        /// <summary>
        /// CreateIssue persists a new object under the workspace key sequence, stamps
        /// the initial To-Do status and the requested priority, subscribes the
        /// requester as watcher, and raises <see cref="IPortalManager.IssueCreated"/>.
        /// </summary>
        [Fact]
        public void CreateIssue_PersistsStampsAndNotifies()
        {
            var manager = Seed(nameof(CreateIssue_PersistsStampsAndNotifies));

            IIssue? created = null;
            manager.IssueCreated += (_, issue) => created = issue;

            var result = manager.CreateIssue("sd-incident", "self-service-form", "VPN drops the connection", "Drops every few minutes.", "P2");

            Assert.NotNull(result);
            // SD-1/SD-2 are portal issues, SD-3 is the internal problem record — the
            // key allocator scans the whole workspace, so the next free key is SD-4.
            Assert.Equal("SD-4", result.Key);
            Assert.Equal(PortalIssueState.Open, result.PortalState);
            Assert.Equal("P2", result.Priority);
            Assert.Single(result.Watchers);
            Assert.Equal("Admin User", result.Watchers[0].Name);
            Assert.NotNull(created);
            Assert.Equal(result.Key, created.Key);

            // the issue is immediately visible in both scopes
            Assert.Contains(manager.GetIssues(IssueScope.Mine), i => i.Key == result.Key);
        }

        /// <summary>
        /// CreateIssue validates its required arguments and rejects unknown request
        /// types.
        /// </summary>
        [Fact]
        public void CreateIssue_ValidatesArguments()
        {
            var manager = Seed(nameof(CreateIssue_ValidatesArguments));

            Assert.ThrowsAny<ArgumentException>(() => manager.CreateIssue(null!, null, "title", null, null));
            Assert.ThrowsAny<ArgumentException>(() => manager.CreateIssue("sd-incident", null, " ", null, null));
            Assert.Throws<InvalidOperationException>(() => manager.CreateIssue("unknown", null, "title", null, null));
        }

        /// <summary>
        /// AddComment persists the comment against the underlying object, surfaces it
        /// in the projected timeline, and raises the comment and update events.
        /// </summary>
        [Fact]
        public void AddComment_AppendsToTimeline()
        {
            var manager = Seed(nameof(AddComment_AppendsToTimeline));

            IssueComment? commented = null;
            var updated = 0;
            manager.IssueCommented += (_, comment) => commented = comment;
            manager.IssueUpdated += (_, _) => updated++;

            var issue = manager.AddComment("SD-1", "Restarted Outlook, no effect.", "public");

            Assert.NotNull(issue);
            var entry = Assert.Single(issue.Comments);
            Assert.Equal("Restarted Outlook, no effect.", entry.Text);
            Assert.Equal("Requester", entry.Role);
            Assert.NotNull(commented);
            Assert.Equal(entry.Id, commented.Id);
            Assert.Equal(1, updated);
        }

        /// <summary>
        /// AddComment persists the requested audience on the comment row and echoes it
        /// back on the projected timeline entry, rather than storing every comment as
        /// public.
        /// </summary>
        [Fact]
        public void AddComment_PersistsVisibility()
        {
            var connectionString = nameof(AddComment_PersistsVisibility);
            var manager = Seed(connectionString);

            manager.AddComment("SD-1", "Checked the mailbox quota.", "internal-team");

            using var db = PortalHubFixture.CreateDbContext(connectionString);
            var stored = db.Comments.Single(c => c.ObjectId == MineObjectId);
            Assert.Equal(CommentVisibility.InternalTeam, stored.Visibility);

            var issue = manager.GetIssue("SD-1");
            var entry = Assert.Single(issue!.Comments);
            Assert.Equal("internal-team", entry.Visibility);
        }

        /// <summary>
        /// An unrecognised visibility token widens to public instead of narrowing, so a
        /// misspelled flag never hides a comment the requester was meant to read.
        /// </summary>
        [Fact]
        public void AddComment_UnknownVisibilityReadsAsPublic()
        {
            var connectionString = nameof(AddComment_UnknownVisibilityReadsAsPublic);
            var manager = Seed(connectionString);

            manager.AddComment("SD-1", "Any update?", "internal_team");

            using var db = PortalHubFixture.CreateDbContext(connectionString);
            Assert.Equal(CommentVisibility.Public, db.Comments.Single(c => c.ObjectId == MineObjectId).Visibility);
        }

        /// <summary>
        /// The projected timeline drops an internal-team comment for a viewer who is
        /// neither the requester, the assignee, nor its author — the audience the portal
        /// concept limits such a comment to — while public comments stay.
        /// </summary>
        [Fact]
        public void GetIssue_FiltersInternalCommentsForUninvolvedViewer()
        {
            var connectionString = nameof(GetIssue_FiltersInternalCommentsForUninvolvedViewer);
            var manager = Seed(connectionString);

            // SD-2 was raised by the second tenant member and carries no assignee, so the
            // acting portal identity is none of the three audiences of an internal note.
            AddComment(connectionString, ForeignObjectId, MemberIdentityId, "Visible to everyone.", CommentVisibility.Public);
            AddComment(connectionString, ForeignObjectId, MemberIdentityId, "Service team only.", CommentVisibility.InternalTeam);

            var issue = manager.GetIssue("SD-2");

            var entry = Assert.Single(issue!.Comments);
            Assert.Equal("Visible to everyone.", entry.Text);
        }

        /// <summary>
        /// The requester of an issue does see its internal-team comments: the concept
        /// limits them to the assigned service group <em>plus the requester</em>.
        /// </summary>
        [Fact]
        public void GetIssue_KeepsInternalCommentsForRequester()
        {
            var connectionString = nameof(GetIssue_KeepsInternalCommentsForRequester);
            var manager = Seed(connectionString);

            // SD-1 was raised by the acting portal identity, so it is that viewer's own
            // issue and the internal note reaches them.
            AddComment(connectionString, MineObjectId, MemberIdentityId, "Service team only.", CommentVisibility.InternalTeam);

            var issue = manager.GetIssue("SD-1");

            var entry = Assert.Single(issue!.Comments);
            Assert.Equal("Service team only.", entry.Text);
        }

        /// <summary>
        /// Writes a comment straight to the store so a test can author it as an identity
        /// other than the portal's acting one, which <see cref="IPortalManager.AddComment"/>
        /// always uses.
        /// </summary>
        /// <param name="connectionString">The per-test in-memory database name.</param>
        /// <param name="objectId">The object to comment on.</param>
        /// <param name="authorId">The authoring identity.</param>
        /// <param name="text">The message body.</param>
        /// <param name="visibility">The audience of the comment.</param>
        private static void AddComment(string connectionString, Guid objectId, Guid authorId, string text, CommentVisibility visibility)
        {
            using var db = PortalHubFixture.CreateDbContext(connectionString);

            db.Comments.Add(new Comment
            {
                ObjectId = objectId,
                AuthorId = authorId,
                Content = text,
                State = CommentState.Active,
                Visibility = visibility,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });

            db.SaveChanges();
        }

        /// <summary>
        /// ShareIssue persists one share per identity, tolerates duplicates silently,
        /// and raises <see cref="IPortalManager.IssueShared"/> only when something
        /// actually changed.
        /// </summary>
        [Fact]
        public void ShareIssue_IsIdempotentAndRaisesOnce()
        {
            var manager = Seed(nameof(ShareIssue_IsIdempotentAndRaisesOnce));

            var shared = 0;
            manager.IssueShared += (_, _) => shared++;

            var issue = manager.ShareIssue("SD-1", [MemberIdentityId.ToString()]);
            Assert.NotNull(issue);
            Assert.Single(issue.SharedWith);
            Assert.Equal("Anna Becker", issue.SharedWith[0].Name);

            issue = manager.ShareIssue("SD-1", [MemberIdentityId.ToString()]);
            Assert.Single(issue.SharedWith);
            Assert.Equal(1, shared);
        }

        /// <summary>
        /// UnshareIssue revokes a granted share and raises
        /// <see cref="IPortalManager.IssueUnshared"/>; revoking a non-existent share
        /// is a silent no-op.
        /// </summary>
        [Fact]
        public void UnshareIssue_RevokesAndRaises()
        {
            var manager = Seed(nameof(UnshareIssue_RevokesAndRaises));

            var unshared = 0;
            manager.IssueUnshared += (_, _) => unshared++;

            manager.ShareIssue("SD-1", [MemberIdentityId.ToString()]);
            var issue = manager.UnshareIssue("SD-1", MemberIdentityId.ToString());

            Assert.NotNull(issue);
            Assert.Empty(issue.SharedWith);
            Assert.Equal(1, unshared);

            issue = manager.UnshareIssue("SD-1", MemberIdentityId.ToString());
            Assert.Empty(issue.SharedWith);
            Assert.Equal(1, unshared);
        }

        /// <summary>
        /// Watch subscribes the current identity exactly once; Unwatch removes the
        /// subscription. Both raise their dedicated events only on actual change.
        /// </summary>
        [Fact]
        public void Watch_Unwatch_RoundTrip()
        {
            var manager = Seed(nameof(Watch_Unwatch_RoundTrip));

            var watchedEvents = 0;
            var unwatchedEvents = 0;
            manager.IssueWatched += (_, _) => watchedEvents++;
            manager.IssueUnwatched += (_, _) => unwatchedEvents++;

            var issue = manager.Watch("SD-2");
            Assert.NotNull(issue);
            Assert.Contains(issue.Watchers, w => w.Name == "Admin User");
            Assert.Equal(1, watchedEvents);

            issue = manager.Unwatch("SD-2");
            Assert.DoesNotContain(issue.Watchers, w => w.Name == "Admin User");
            Assert.Equal(1, unwatchedEvents);
        }

        /// <summary>
        /// AcceptResolution closes a Resolved issue (terminal Done-category status),
        /// appends the machine narration to the timeline, and raises the acceptance,
        /// closed, and updated events. Accepting a non-Resolved, non-Closed issue is
        /// a state conflict (concept §API: 409).
        /// </summary>
        [Fact]
        public void AcceptResolution_ClosesResolvedIssue()
        {
            var connectionString = nameof(AcceptResolution_ClosesResolvedIssue);
            var manager = Seed(connectionString);

            var accepted = 0;
            var closed = 0;
            manager.IssueResolutionAccepted += (_, _) => accepted++;
            manager.IssueClosed += (_, _) => closed++;

            // not yet resolved — accepting must surface a 409 conflict
            Assert.Throws<PortalConflictException>(() => manager.AcceptResolution("SD-1"));
            Assert.Equal(0, accepted);

            StampStatus(connectionString, MineObjectId, "Resolved");

            var issue = manager.AcceptResolution("SD-1");

            Assert.Equal(PortalIssueState.Closed, issue!.PortalState);
            Assert.Contains(issue.Comments, c => c.Text.Contains("Resolution accepted"));
            Assert.Equal(1, accepted);
            Assert.Equal(1, closed);

            // idempotent: re-accepting a closed issue returns the projection
            // unchanged and fires no new events.
            var second = manager.AcceptResolution("SD-1");
            Assert.Equal(PortalIssueState.Closed, second!.PortalState);
            Assert.Equal(1, accepted);
            Assert.Equal(1, closed);
        }

        /// <summary>
        /// RejectResolution demands a reason, returns the issue to In Progress, and
        /// appends the reason to the timeline. Rejecting a non-Resolved issue is a
        /// state conflict (concept §API: 409).
        /// </summary>
        [Fact]
        public void RejectResolution_ReturnsIssueToInProgress()
        {
            var connectionString = nameof(RejectResolution_ReturnsIssueToInProgress);
            var manager = Seed(connectionString);

            Assert.ThrowsAny<ArgumentException>(() => manager.RejectResolution("SD-1", " "));

            // rejecting a non-Resolved issue must surface a 409 conflict
            Assert.Throws<PortalConflictException>(() => manager.RejectResolution("SD-1", "No."));

            StampStatus(connectionString, MineObjectId, "Resolved");

            var rejected = 0;
            manager.IssueResolutionRejected += (_, _) => rejected++;

            var issue = manager.RejectResolution("SD-1", "Mail still not arriving.");

            Assert.Equal(PortalIssueState.InProgress, issue!.PortalState);
            Assert.Contains(issue.Comments, c => c.Text.Contains("Mail still not arriving."));
            Assert.Equal(1, rejected);
        }

        /// <summary>
        /// Unwatch without an active subscription is a state conflict (concept §API:
        /// 409). The state-of-the-art manager raises a <see cref="PortalConflictException"/>
        /// so the REST layer can map it to a <c>409/422</c> response.
        /// </summary>
        [Fact]
        public void Unwatch_WithoutActiveSubscription_ThrowsConflict()
        {
            var connectionString = nameof(Unwatch_WithoutActiveSubscription_ThrowsConflict);
            var manager = Seed(connectionString);

            Assert.Throws<PortalConflictException>(() => manager.Unwatch("SD-1"));
        }

        /// <summary>
        /// <see cref="IPortalManager.NotifyResolutionProposed"/> raises the event
        /// for an object whose workflow status collapses to <c>Resolved</c>, and
        /// stays silent for an object in any other state.
        /// </summary>
        [Fact]
        public void NotifyResolutionProposed_FiresOnlyForResolvedIssues()
        {
            var connectionString = nameof(NotifyResolutionProposed_FiresOnlyForResolvedIssues);
            var manager = Seed(connectionString);

            var raised = 0;
            IIssue? raisedIssue = null;
            manager.IssueResolutionProposed += (_, issue) => { raised++; raisedIssue = issue; };

            // not yet resolved — must not fire
            var miss = manager.NotifyResolutionProposed(MineObjectId);
            Assert.Null(miss);
            Assert.Equal(0, raised);

            StampStatus(connectionString, MineObjectId, "Resolved");

            var hit = manager.NotifyResolutionProposed(MineObjectId);
            Assert.NotNull(hit);
            Assert.Equal(PortalIssueState.Resolved, hit!.PortalState);
            Assert.Equal(1, raised);
            Assert.Same(hit, raisedIssue);
        }

        /// <summary>
        /// The organization directory lists the active identities of the current user's
        /// tenant, ordered by name, and the current user resolves to the fallback (seeded
        /// admin) identity.
        /// </summary>
        [Fact]
        public void CurrentUser_And_OrganizationMembers_ResolveFromIdentityModel()
        {
            var manager = Seed(nameof(CurrentUser_And_OrganizationMembers_ResolveFromIdentityModel));

            var current = manager.CurrentUser;
            Assert.NotNull(current);
            Assert.Equal("Admin User", current.Name);

            var members = manager.GetOrganizationMembers();
            // the seed provisions four identities (admin, Anna, Gina, Sam); the admin is
            // Acme, so the directory is Acme - Gina (Globex) and Sam (no tenant) are not in it
            Assert.Equal(2, members.Count);
            Assert.Equal("Admin User", members[0].Name);
            Assert.Equal("Anna Becker", members[1].Name);
        }

        /// <summary>
        /// The directory is bounded by the caller's tenant: a Globex member sees Globex, an
        /// operator-side account without a tenant sees nobody, and so does a caller the
        /// system does not know. The no-caller overload stands in the seeded admin.
        /// </summary>
        [Fact]
        public void GetOrganizationMembers_IsBoundedByTheCallersTenant()
        {
            var manager = Seed(nameof(GetOrganizationMembers_IsBoundedByTheCallersTenant));

            var globex = manager.GetOrganizationMembers(GlobexMemberIdentityId);
            Assert.Single(globex);
            Assert.Equal("Gina Globex", globex[0].Name);

            Assert.Empty(manager.GetOrganizationMembers(TenantlessIdentityId));
            Assert.Empty(manager.GetOrganizationMembers(Guid.NewGuid()));
            Assert.Empty(manager.GetOrganizationMembers(Guid.Empty));

            var fallback = manager.GetOrganizationMembers(null);
            Assert.Equal(2, fallback.Count);
            Assert.Equal("Admin User", fallback[0].Name);
            Assert.Equal("Anna Becker", fallback[1].Name);
        }

        /// <summary>
        /// A disabled account is not in the directory even inside the tenant.
        /// </summary>
        [Fact]
        public void GetOrganizationMembers_LeavesOutDisabledAccounts()
        {
            var manager = Seed(nameof(GetOrganizationMembers_LeavesOutDisabledAccounts));

            using (var db = PortalHubFixture.CreateDbContext(nameof(GetOrganizationMembers_LeavesOutDisabledAccounts)))
            {
                var anna = db.Identities.First(i => i.Id == MemberIdentityId);
                anna.State = IdentityState.Disabled;
                db.SaveChanges();
            }

            var members = manager.GetOrganizationMembers();
            Assert.Single(members);
            Assert.Equal("Admin User", members[0].Name);
        }

        /// <summary>
        /// A share is bounded the way the directory is: an identity of another tenant, an
        /// operator account and an unparsable id are skipped, and the issue is only touched
        /// when something was actually shared.
        /// </summary>
        [Fact]
        public void ShareIssue_RefusesIdentitiesOutsideTheOrganization()
        {
            var manager = Seed(nameof(ShareIssue_RefusesIdentitiesOutsideTheOrganization));

            var raised = 0;
            manager.IssueShared += (_, _) => raised++;

            var issue = manager.ShareIssue("SD-1", [GlobexMemberIdentityId.ToString(), TenantlessIdentityId.ToString(), "not-a-guid"]);

            Assert.NotNull(issue);
            Assert.Empty(issue.SharedWith);
            Assert.Equal(0, raised);

            issue = manager.ShareIssue("SD-1", [GlobexMemberIdentityId.ToString(), MemberIdentityId.ToString()]);

            Assert.Single(issue!.SharedWith);
            Assert.Equal("Anna Becker", issue.SharedWith[0].Name);
            Assert.Equal(1, raised);
        }

        /// <summary>
        /// Two workspaces may both offer an "Incident": the request-type key carries the
        /// workspace key, so the two are distinct and each is addressed by its own key. The
        /// bare class name addresses nothing any more.
        /// </summary>
        [Fact]
        public void RequestTypeKeys_AreUniqueAcrossWorkspaces()
        {
            var manager = Seed(nameof(RequestTypeKeys_AreUniqueAcrossWorkspaces));

            var keys = manager.GetRequestTypes().Select(rt => rt.Key).ToList();

            Assert.Equal(2, keys.Count);
            Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Contains("sd-incident", keys);
            Assert.Contains("sdg-incident", keys);

            Assert.Equal("Incident management.", manager.GetRequestType("sd-incident")!.Description);
            Assert.Equal("Incident management (Globex).", manager.GetRequestType("sdg-incident")!.Description);
            Assert.Null(manager.GetRequestType("incident"));
        }

        /// <summary>
        /// Creating an issue against the Globex request type files it in the Globex
        /// workspace, under that workspace's key sequence, although the class carries the
        /// same name as the Acme one.
        /// </summary>
        [Fact]
        public void CreateIssue_AddressesTheRequestTypeByWorkspace()
        {
            var manager = Seed(nameof(CreateIssue_AddressesTheRequestTypeByWorkspace));

            var result = manager.CreateIssue("SDG-Incident", null, "Globex mail down", null, null);

            Assert.NotNull(result);
            Assert.Equal("SDG-2", result.Key);
            Assert.Equal("Incident management (Globex).", result.RequestType?.Description);

            Assert.Throws<InvalidOperationException>(() => manager.CreateIssue("incident", null, "no workspace named", null, null));
        }

        /// <summary>
        /// Seeds an active workflow on the Incident class - every seeded status takes part,
        /// New is the entry, and the moves the cases need are declared - and binds the
        /// workflow field to it.
        /// </summary>
        /// <param name="connectionString">The per-test in-memory database name.</param>
        /// <returns>The status ids by name.</returns>
        private static Dictionary<string, Guid> SeedWorkflow(string connectionString)
        {
            using var db = PortalHubFixture.CreateDbContext(connectionString);

            var statuses = db.Statuses
                .Where(s => s.ClassId == IncidentClassId)
                .ToDictionary(s => s.Name, s => s.Id);

            var workflowId = Guid.NewGuid();

            db.Workflows.Add(new Workflow
            {
                Id = workflowId,
                Name = "Incident Lifecycle",
                ClassId = IncidentClassId,
                State = WorkflowState.Active,
                WorkflowStatuses = [.. statuses.Select(s => new WorkflowStatus { StatusId = s.Value, IsStart = s.Key == "New" })]
            });

            void addTransition(string name, string from, string to) => db.Transitions.Add(new Transition
            {
                Name = name,
                WorkflowId = workflowId,
                SourceId = statuses[from],
                TargetId = statuses[to],
                State = TransitionState.Active
            });

            addTransition("Start", "New", "In Progress");
            addTransition("Ask", "In Progress", "Waiting on Requester");
            addTransition("Resolve", "In Progress", "Resolved");
            addTransition("Resolve again", "Waiting on Requester", "Resolved");
            addTransition("Close", "Resolved", "Closed");

            var field = db.Fields.First(f => f.Id == WorkflowFieldId);
            field.WorkflowId = workflowId;

            db.SaveChanges();

            return statuses;
        }

        /// <summary>
        /// Once connected, an operator-side workflow move that stamps a portal issue with a
        /// resolved state raises the portal's resolution event with the projected issue; a
        /// move to any other state does not.
        /// </summary>
        [Fact]
        public void Connect_RaisesResolutionProposedFromAWorkflowMove()
        {
            var manager = Seed(nameof(Connect_RaisesResolutionProposedFromAWorkflowMove));
            var statuses = SeedWorkflow(nameof(Connect_RaisesResolutionProposedFromAWorkflowMove));

            var proposed = new List<IIssue>();
            manager.IssueResolutionProposed += (_, issue) => proposed.Add(issue);

            manager.Connect();

            // SD-2 is New: starting work is not a proposal
            var started = CoreHub.WorkflowManager.ExecuteTransition(ForeignObjectId, WorkflowFieldId, statuses["In Progress"], PortalManager.FallbackIdentityId);
            Assert.True(started.Succeeded);
            Assert.Empty(proposed);

            // SD-1 is In Progress: resolving it is
            var resolved = CoreHub.WorkflowManager.ExecuteTransition(MineObjectId, WorkflowFieldId, statuses["Resolved"], PortalManager.FallbackIdentityId);
            Assert.True(resolved.Succeeded);

            var issue = Assert.Single(proposed);
            Assert.Equal("SD-1", issue.Key);
            Assert.Equal(PortalIssueState.Resolved, issue.PortalState);

            // closing is terminal, not a proposal
            var closed = CoreHub.WorkflowManager.ExecuteTransition(MineObjectId, WorkflowFieldId, statuses["Closed"], PortalManager.FallbackIdentityId);
            Assert.True(closed.Succeeded);
            Assert.Single(proposed);
        }

        /// <summary>
        /// A move that the workflow refuses raises nothing, and connecting twice subscribes
        /// once.
        /// </summary>
        [Fact]
        public void Connect_IsIdempotentAndIgnoresRefusedMoves()
        {
            var manager = Seed(nameof(Connect_IsIdempotentAndIgnoresRefusedMoves));
            var statuses = SeedWorkflow(nameof(Connect_IsIdempotentAndIgnoresRefusedMoves));

            var proposed = 0;
            manager.IssueResolutionProposed += (_, _) => proposed++;

            manager.Connect();
            manager.Connect();

            // SD-2 is New and the workflow declares no move from New to Resolved
            var refused = CoreHub.WorkflowManager.ExecuteTransition(ForeignObjectId, WorkflowFieldId, statuses["Resolved"], PortalManager.FallbackIdentityId);
            Assert.False(refused.Succeeded);
            Assert.Equal(0, proposed);

            CoreHub.WorkflowManager.ExecuteTransition(MineObjectId, WorkflowFieldId, statuses["Resolved"], PortalManager.FallbackIdentityId);
            Assert.Equal(1, proposed);
        }

        /// <summary>
        /// Workspace list is tenant-isolated: an Acme member sees the Acme
        /// workspace; the seeded admin (also Acme) sees it too; the tenant-less
        /// operator identity sees every active workspace.
        /// </summary>
        [Fact]
        public void GetWorkspaces_RespectsTenantScope()
        {
            var manager = Seed(nameof(GetWorkspaces_RespectsTenantScope));

            var acmeView = manager.GetWorkspaces(PortalManager.FallbackIdentityId);
            var globexView = manager.GetWorkspaces(GlobexMemberIdentityId);
            var operatorView = manager.GetWorkspaces(TenantlessIdentityId);

            Assert.Single(acmeView);
            Assert.Equal("SD", acmeView[0].Key);

            Assert.Single(globexView);
            Assert.Equal("SDG", globexView[0].Key);

            // operator identity (no tenant) sees every active workspace
            Assert.Equal(2, operatorView.Count);
        }

        /// <summary>
        /// Class detail is resolved by id and the portal-visible toggle flips
        /// the flag in place. The flag round-trips through the manager.
        /// </summary>
        [Fact]
        public void GetClass_AndTogglePortalVisible_RoundTrip()
        {
            var manager = Seed(nameof(GetClass_AndTogglePortalVisible_RoundTrip));

            var cls = manager.GetClass(IncidentClassId);
            Assert.NotNull(cls);
            Assert.True(cls.PortalVisible);

            // toggle off
            var after = manager.TogglePortalVisible(IncidentClassId);
            Assert.False(after!.PortalVisible);

            // toggle back on
            var afterAgain = manager.TogglePortalVisible(IncidentClassId);
            Assert.True(afterAgain!.PortalVisible);
        }

        /// <summary>
        /// Field CRUD: add a field, list it, update it, clone it, then soft-delete
        /// it via the deprecated flag.
        /// </summary>
        [Fact]
        public void Field_AddListUpdateCloneDelete_RoundTrip()
        {
            var manager = Seed(nameof(Field_AddListUpdateCloneDelete_RoundTrip));

            var initial = manager.GetFields(IncidentClassId);
            // the seed provisions two fields (Status, Priority)
            Assert.Equal(2, initial.Count);

            var created = manager.AddField(IncidentClassId, "Email", "Contact e-mail address.", FieldType.Text, FieldCardinality.Single, required: true, uniqueConstraint: false);
            Assert.NotNull(created);
            Assert.Equal("Email", created.Name);
            Assert.True(created.Required);

            var listed = manager.GetFields(IncidentClassId);
            Assert.Equal(3, listed.Count);
            Assert.Contains(listed, f => f.Id == created.Id);

            var updated = manager.UpdateField(created.Id, "E-mail", "Primary e-mail.", FieldType.Text, FieldCardinality.Multiple, required: false, uniqueConstraint: true);
            Assert.NotNull(updated);
            Assert.Equal("E-mail", updated!.Name);
            Assert.Equal(FieldCardinality.Multiple, updated.Cardinality);
            Assert.False(updated.Required);
            Assert.True(updated.Unique);

            var clone = manager.CloneField(created.Id);
            Assert.NotNull(clone);
            Assert.Equal("E-mail (copy)", clone!.Name);

            var removed = manager.DeleteField(created.Id);
            Assert.True(removed);

            var afterDelete = manager.GetFields(IncidentClassId);
            Assert.DoesNotContain(afterDelete, f => f.Id == created.Id);
        }

        /// <summary>
        /// Reserved field names are rejected.
        /// </summary>
        [Fact]
        public void AddField_RejectsReservedNames()
        {
            var manager = Seed(nameof(AddField_RejectsReservedNames));

            Assert.Throws<InvalidOperationException>(() =>
                manager.AddField(IncidentClassId, "admin", "system reserved", FieldType.Text, FieldCardinality.Single, false, false));
        }

        /// <summary>
        /// Form portal-template toggle round-trips.
        /// </summary>
        [Fact]
        public void TogglePortalTemplate_RoundTrips()
        {
            var manager = Seed(nameof(TogglePortalTemplate_RoundTrips));

            // the seed provisions two forms (Self-Service Form, Resolver Form);
            // pick the first one and round-trip the toggle.
            var forms = manager.GetForms(IncidentClassId);
            Assert.Equal(2, forms.Count);
            var formId = forms[0].Id;
            var before = forms[0].PortalTemplate;

            var flipped = manager.TogglePortalTemplate(formId);
            Assert.NotEqual(before, flipped!.PortalTemplate);

            var flippedAgain = manager.TogglePortalTemplate(formId);
            Assert.Equal(before, flippedAgain!.PortalTemplate);
        }
    }
}
