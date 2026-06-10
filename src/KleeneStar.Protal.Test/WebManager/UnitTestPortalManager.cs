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

            db.Workspaces.Add(new Workspace { Id = WorkspaceId, Key = "SD", Name = "IT Service Desk" });

            db.Classes.Add(new Class
            {
                Id = IncidentClassId,
                Name = "Incident",
                Description = "Incident management.",
                WorkspaceId = WorkspaceId,
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
                PasswordHash = "$test$"
            });
            db.Identities.Add(new Identity(MemberIdentityId)
            {
                Name = "Anna Becker",
                Email = "anna.becker@kleenestar.org",
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

            var requestType = Assert.Single(requestTypes);
            Assert.Equal("incident", requestType.Key);
            Assert.Equal("Incident", requestType.Title);

            var template = Assert.Single(requestType.Templates);
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

            Assert.NotNull(manager.GetRequestType("Incident"));
            Assert.NotNull(manager.GetRequestType("INCIDENT"));
            Assert.Null(manager.GetRequestType("problem"));
        }

        /// <summary>
        /// The organization scope lists every issue of every portal-visible class —
        /// and nothing from internal classes.
        /// </summary>
        [Fact]
        public void GetIssues_OrganizationScope_ExcludesInternalClasses()
        {
            var manager = Seed(nameof(GetIssues_OrganizationScope_ExcludesInternalClasses));

            var issues = manager.GetIssues(IssueScope.Organization);

            Assert.Equal(2, issues.Count);
            Assert.DoesNotContain(issues, i => i.Key == "SD-3");
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

            var result = manager.CreateIssue("incident", "self-service-form", "VPN drops the connection", "Drops every few minutes.", "P2");

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
            Assert.ThrowsAny<ArgumentException>(() => manager.CreateIssue("incident", null, " ", null, null));
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
        /// closed, and updated events. Accepting a non-Resolved issue is a no-op.
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

            // not yet resolved — accepting must not change anything
            var unchanged = manager.AcceptResolution("SD-1");
            Assert.Equal(PortalIssueState.InProgress, unchanged!.PortalState);
            Assert.Equal(0, accepted);

            StampStatus(connectionString, MineObjectId, "Resolved");

            var issue = manager.AcceptResolution("SD-1");

            Assert.Equal(PortalIssueState.Closed, issue!.PortalState);
            Assert.Contains(issue.Comments, c => c.Text.Contains("Resolution accepted"));
            Assert.Equal(1, accepted);
            Assert.Equal(1, closed);
        }

        /// <summary>
        /// RejectResolution demands a reason, returns the issue to In Progress, and
        /// appends the reason to the timeline.
        /// </summary>
        [Fact]
        public void RejectResolution_ReturnsIssueToInProgress()
        {
            var connectionString = nameof(RejectResolution_ReturnsIssueToInProgress);
            var manager = Seed(connectionString);

            Assert.ThrowsAny<ArgumentException>(() => manager.RejectResolution("SD-1", " "));

            StampStatus(connectionString, MineObjectId, "Resolved");

            var rejected = 0;
            manager.IssueResolutionRejected += (_, _) => rejected++;

            var issue = manager.RejectResolution("SD-1", "Mail still not arriving.");

            Assert.Equal(PortalIssueState.InProgress, issue!.PortalState);
            Assert.Contains(issue.Comments, c => c.Text.Contains("Mail still not arriving."));
            Assert.Equal(1, rejected);
        }

        /// <summary>
        /// The organization directory lists every active identity, ordered by name,
        /// and the current user resolves to the fallback (seeded admin) identity.
        /// </summary>
        [Fact]
        public void CurrentUser_And_OrganizationMembers_ResolveFromIdentityModel()
        {
            var manager = Seed(nameof(CurrentUser_And_OrganizationMembers_ResolveFromIdentityModel));

            var current = manager.CurrentUser;
            Assert.NotNull(current);
            Assert.Equal("Admin User", current.Name);

            var members = manager.GetOrganizationMembers();
            Assert.Equal(2, members.Count);
            Assert.Equal("Admin User", members[0].Name);
            Assert.Equal("Anna Becker", members[1].Name);
        }
    }
}
