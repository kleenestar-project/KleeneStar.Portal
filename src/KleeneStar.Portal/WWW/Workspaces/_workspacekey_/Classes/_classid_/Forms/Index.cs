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

namespace KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes._classid_.Forms
{
    /// <summary>
    /// Forms tab — read-only list of the active forms of a class, with a
    /// portal-template toggle on each row.
    /// </summary>
    [WebIcon<RequestTypeIcon>]
    [ClassIdSegment]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline.</summary>
        public const string HeadlineResource = "kleenestar.portal:admin.forms.headline";

        /// <summary>Resource key for the empty-state message.</summary>
        public const string EmptyResource = "kleenestar.portal:admin.forms.empty";

        /// <summary>Resource key for the name column.</summary>
        public const string NameColumnResource = "kleenestar.portal:admin.forms.column.name";

        /// <summary>Resource key for the description column.</summary>
        public const string DescriptionColumnResource = "kleenestar.portal:admin.forms.column.description";

        /// <summary>Resource key for the portal-template column.</summary>
        public const string PortalTemplateColumnResource = "kleenestar.portal:admin.forms.column.portal-template";

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
            if (classIdParameter is null || !Guid.TryParse(classIdParameter.Value, out var classId)
                || _portalManager.GetClass(classId) is null)
            {
                visualTree.Title = HeadlineResource;
                visualTree.Content.MainPanel.Headline.Title = HeadlineResource;
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => "kleenestar.portal:admin.class.not-found",
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            var forms = _portalManager.GetForms(classId);
            visualTree.Title = HeadlineResource;
            visualTree.Content.MainPanel.Headline.Title = HeadlineResource;

            if (forms.Count == 0)
            {
                visualTree.Content.MainPanel.AddPrimary(new ControlText()
                {
                    Text = _ => EmptyResource,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                });
                return;
            }

            var table = new ControlTable() { Striped = _ => TypeStripedTable.Row }
                .AddColumn(NameColumnResource)
                .AddColumn(DescriptionColumnResource)
                .AddColumn(PortalTemplateColumnResource);

            var classIdParam = new ClassIdParameter(classId.ToString());

            foreach (var form in forms)
            {
                var capturedId = form.Id;
                var capturedName = form.Name;
                var capturedDescription = form.Description ?? string.Empty;
                var toggleUri = PortalHub
                    .GetUri<global::KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Forms._formid_.PortalTemplate>()
                    ?.BindParameters(classIdParam)
                    ?.BindParameters(new FormIdParameter(capturedId.ToString()));

                var toggleForm = new ControlModalForm($"portal-form-portaltemplate-form-{capturedId}")
                {
                    Uri = _ => toggleUri,
                    Method = _ => WebExpress.WebCore.WebMessage.RequestMethod.POST
                };
                toggleForm.AddPrimaryButton(new ControlFormItemButtonSubmit()
                {
                    Text = _ => form.PortalTemplate
                        ? "kleenestar.portal:admin.forms.action.unset-template"
                        : "kleenestar.portal:admin.forms.action.set-template"
                });

                table = table.AddRow
                (
                    new ControlTableCell() { Text = _ => capturedName },
                    new ControlTableCellPanel().Add(new ControlText()
                    {
                        Text = _ => capturedDescription,
                        TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                    }),
                    new ControlTableCellPanel().Add(toggleForm)
                );
            }

            visualTree.Content.MainPanel.AddPrimary(table);
        }
    }
}
