using KleeneStar.Core.WebAttribute;
using KleeneStar.Core.WebParameter;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebScope;
using System;
using System.Linq;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes._classid_
{
    /// <summary>
    /// Class detail page — the central service-request-type editor. Shows the
    /// class's metadata, exposes the portal-visible toggle, and links to the
    /// per-tab sub-pages (Fields, Statuses, Priorities, Forms).
    /// </summary>
    [WebIcon<RequestTypeIcon>]
    [ClassIdSegment]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline.</summary>
        public const string HeadlineResource = "kleenestar.portal:admin.class.headline";

        /// <summary>Resource key for the not-found message.</summary>
        public const string NotFoundResource = "kleenestar.portal:admin.class.not-found";

        /// <summary>Resource key for the description block.</summary>
        public const string DescriptionResource = "kleenestar.portal:admin.class.description";

        /// <summary>Resource key for the state-pill line.</summary>
        public const string StateResource = "kleenestar.portal:admin.class.state";

        /// <summary>Resource key for the workspace line.</summary>
        public const string WorkspaceResource = "kleenestar.portal:admin.class.workspace";

        /// <summary>Resource key for the portal-visible toggle on.</summary>
        public const string PortalVisibleOnResource = "kleenestar.portal:admin.class.portal-visible.on";

        /// <summary>Resource key for the portal-visible toggle off.</summary>
        public const string PortalVisibleOffResource = "kleenestar.portal:admin.class.portal-visible.off";

        /// <summary>Resource key for the sub-tab labels.</summary>
        public const string FieldsTabResource = "kleenestar.portal:admin.class.tab.fields";
        public const string StatusesTabResource = "kleenestar.portal:admin.class.tab.statuses";
        public const string PrioritiesTabResource = "kleenestar.portal:admin.class.tab.priorities";
        public const string FormsTabResource = "kleenestar.portal:admin.class.tab.forms";

        private readonly IPortalManager _portalManager;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="portalManager">The portal manager.</param>
        public Index(IPortalManager portalManager)
        {
            _portalManager = portalManager;
        }

        /// <summary>
        /// Processing of the resource.
        /// </summary>
        /// <param name="renderContext">The context for rendering the page.</param>
        /// <param name="visualTree">The visual tree of the web application.</param>
        public void Process(IRenderContext renderContext, VisualTreeWebApp visualTree)
        {
            var classIdParameter = renderContext.Request.GetParameter<ClassIdParameter>();
            var cls = _portalManager.GetClass(classIdParameter is null
                ? Guid.Empty
                : (Guid.TryParse(classIdParameter.Value, out var id) ? id : Guid.Empty));

            visualTree.Title = cls?.Name ?? classIdParameter?.Value;
            visualTree.Content.MainPanel.Headline.Title = HeadlineResource;

            if (cls is null)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => NotFoundResource,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            // head: name, description, state
            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => cls.Name,
                TextColor = _ => new PropertyColorText(TypeColorText.Primary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => cls.Description ?? string.Empty,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.None, PropertySpacing.Space.None, PropertySpacing.Space.Three, PropertySpacing.Space.None)
            });

            // portal-visible toggle as a single-cell row of the meta table
            var portalVisibleRestUri = PortalHub
                .GetUri<global::KleeneStar.Portal.WWW.Api._1_.Classes._classid_.PortalVisible>()
                ?.BindParameters(new ClassIdParameter(cls.Id.ToString()));

            var toggleForm = new ControlModalForm("portal-class-portalvisible-form")
            {
                Uri = _ => portalVisibleRestUri,
                Method = _ => WebExpress.WebCore.WebMessage.RequestMethod.POST
            };

            toggleForm.AddPrimaryButton(new ControlFormItemButtonSubmit()
            {
                Text = _ => cls.PortalVisible ? PortalVisibleOffResource : PortalVisibleOnResource
            });
            visualTree.Content.MainPanel.AddPrimary(toggleForm);

            // sub-tab links: Fields / Statuses / Priorities / Forms
            var keyParameter = renderContext.Request.GetParameter<WorkspaceKeyParameter>();

            visualTree.Content.MainPanel.AddPrimary(new ControlText()
            {
                Text = _ => WorkspaceResource,
                TextColor = _ => new PropertyColorText(TypeColorText.Secondary),
                Margin = _ => new PropertySpacingMargin(PropertySpacing.Space.Three, PropertySpacing.Space.None, PropertySpacing.Space.One, PropertySpacing.Space.None)
            });

            var subTable = new ControlTable() { Striped = _ => TypeStripedTable.Row }
                .AddColumn("");

            subTable = subTable.AddRow
            (
                new ControlTableCellPanel().Add(new ControlLink()
                {
                    Text = _ => FieldsTabResource,
                    Uri = _ => PortalHub
                        .GetUri<global::KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes._classid_.Fields.Index>()
                        ?.BindParameters(new WorkspaceKeyParameter(keyParameter?.Value))
                        ?.BindParameters(new ClassIdParameter(cls.Id.ToString()))
                })
            );

            subTable = subTable.AddRow
            (
                new ControlTableCellPanel().Add(new ControlLink()
                {
                    Text = _ => StatusesTabResource,
                    Uri = _ => PortalHub
                        .GetUri<global::KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes._classid_.Statuses.Index>()
                        ?.BindParameters(new WorkspaceKeyParameter(keyParameter?.Value))
                        ?.BindParameters(new ClassIdParameter(cls.Id.ToString()))
                })
            );

            subTable = subTable.AddRow
            (
                new ControlTableCellPanel().Add(new ControlLink()
                {
                    Text = _ => PrioritiesTabResource,
                    Uri = _ => PortalHub
                        .GetUri<global::KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes._classid_.Priorities.Index>()
                        ?.BindParameters(new WorkspaceKeyParameter(keyParameter?.Value))
                        ?.BindParameters(new ClassIdParameter(cls.Id.ToString()))
                })
            );

            subTable = subTable.AddRow
            (
                new ControlTableCellPanel().Add(new ControlLink()
                {
                    Text = _ => FormsTabResource,
                    Uri = _ => PortalHub
                        .GetUri<global::KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes._classid_.Forms.Index>()
                        ?.BindParameters(new WorkspaceKeyParameter(keyParameter?.Value))
                        ?.BindParameters(new ClassIdParameter(cls.Id.ToString()))
                })
            );

            visualTree.Content.MainPanel.AddPrimary(subTable);
        }
    }
}
