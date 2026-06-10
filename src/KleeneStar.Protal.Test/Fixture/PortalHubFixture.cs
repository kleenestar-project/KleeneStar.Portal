using KleeneStar.Core;
using KleeneStar.Core.WebManager;
using KleeneStar.Model;
using KleeneStar.Model.Config;
using KleeneStar.Portal.WebManager;
using System.Reflection;
using WebExpress.WebCore;
using WebExpress.WebCore.WebComponent;

namespace KleeneStar.Portal.Test
{
    /// <summary>
    /// Test fixture that wires <see cref="CoreHub"/> and <see cref="ModelHub"/> to an
    /// isolated in-memory database, pre-populates the private manager backing fields so
    /// that <see cref="CoreHub.ClassManager"/> and siblings resolve without a real
    /// <see cref="IComponentHub"/>, and constructs a <see cref="PortalManager"/>
    /// instance for direct use in tests.
    /// </summary>
    internal static class PortalHubFixture
    {
        private static readonly (string FieldName, Type ManagerType)[] _managers =
        [
            ("_workspaceManager", typeof(WorkspaceManager)),
            ("_classManager",     typeof(ClassManager)),
            ("_fieldManager",     typeof(FieldManager)),
            ("_formManager",      typeof(FormManager)),
            ("_priorityManager",  typeof(PriorityManager)),
            ("_workflowManager",  typeof(WorkflowManager)),
            ("_statusManager",    typeof(StatusManager)),
            ("_objectManager",    typeof(ObjectManager)),
            ("_dashboardManager", typeof(DashboardManager)),
            ("_tenantManager",    typeof(TenantManager)),
            ("_identityManager",  typeof(IdentityManager)),
            ("_groupManager",     typeof(GroupManager)),
            ("_slaManager",       typeof(SlaManager)),
            ("_calendarManager",  typeof(CalendarManager)),
            ("_commentManager",   typeof(CommentManager)),
            ("_attachmentManager",typeof(AttachmentManager)),
            ("_watcherManager",   typeof(WatcherManager)),
            ("_shareManager",     typeof(ShareManager)),
            ("_objectTagManager", typeof(ObjectTagManager)),
            ("_valueManager",     typeof(ValueManager)),
            ("_templateManager",  typeof(TemplateManager)),
            ("_objectViewManager",typeof(ObjectViewManager)),
            ("_objectLinkManager",typeof(ObjectLinkManager)),
            ("_sessionManager",   typeof(SessionManager)),
        ];

        /// <summary>
        /// Points <see cref="ModelHub"/> at an isolated in-memory database, seeds
        /// <see cref="CoreHub"/>'s private manager backing fields, and constructs the
        /// <see cref="PortalManager"/> under test.
        /// </summary>
        /// <param name="connectionString">
        /// A unique per-test connection string so parallel-phase state does not leak
        /// between cases in the same <c>[Collection("NonParallelTests")]</c> collection.
        /// </param>
        /// <returns>The portal manager under test.</returns>
        public static PortalManager Initialize(string connectionString)
        {
            ModelHub.DatabaseConfig = new DbConfig
            {
                Assembly = "KleeneStar.Protal.Test",
                ConnectionString = connectionString
            };

            foreach (var (fieldName, managerType) in _managers)
            {
                var instance = Construct(managerType);

                var field = typeof(CoreHub).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException($"Backing field {fieldName} not found on CoreHub.");

                field.SetValue(null, instance);
            }

            return (PortalManager)Construct(typeof(PortalManager));
        }

        /// <summary>
        /// Creates a DbContext against the same in-memory database that
        /// <see cref="ModelHub"/> is routed to, for use in the <c>arrange</c> phase
        /// when seeding entities.
        /// </summary>
        /// <param name="connectionString">The in-memory database name.</param>
        /// <returns>A configured KleeneStarDbContext instance.</returns>
        public static KleeneStarDbContext CreateDbContext(string connectionString)
        {
            return InMemoryDbContextFactory.Create(connectionString);
        }

        /// <summary>
        /// Invokes the private WebExpress-style constructor
        /// <c>(IComponentHub, IHttpServerContext)</c> of the supplied manager type.
        /// </summary>
        /// <param name="managerType">The manager type to construct.</param>
        /// <returns>The constructed instance.</returns>
        private static object Construct(Type managerType)
        {
            var ctor = managerType.GetConstructor
            (
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(IComponentHub), typeof(IHttpServerContext)],
                null
            ) ?? throw new InvalidOperationException($"Private ctor not found on {managerType.FullName}.");

            return ctor.Invoke([null, null]);
        }
    }
}
