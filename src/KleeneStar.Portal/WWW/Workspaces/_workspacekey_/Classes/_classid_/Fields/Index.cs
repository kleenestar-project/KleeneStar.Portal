using KleeneStar.Core.WebAttribute;
using KleeneStar.Core.WebParameter;
using KleeneStar.Portal.WebIcon;
using KleeneStar.Portal.WebManager;
using KleeneStar.Portal.WebScope;
using System;
using System.Linq;
using WebExpress.WebApp.WebPage;
using WebExpress.WebCore.Internationalization;
using WebExpress.WebCore.WebAttribute;
using WebExpress.WebCore.WebPage;
using WebExpress.WebUI.WebControl;

namespace KleeneStar.Portal.WWW.Workspaces._workspacekey_.Classes._classid_.Fields
{
    /// <summary>
    /// Fields tab — lists the active fields of a class as a table with edit /
    /// clone / delete actions. The <c>{workspacekey}</c> and <c>{classid}</c>
    /// URL segments are bound by their respective segment attributes; the
    /// fragment <see cref="FieldAddButtonFragment"/> renders the "Add field"
    /// action.
    /// </summary>
    [WebIcon<RequestTypeIcon>]
    [ClassIdSegment]
    [Scope<IScopePortal>]
    [Cache]
    public sealed class Index : IPage<VisualTreeWebApp>, IScopePortal
    {
        /// <summary>Resource key for the headline.</summary>
        public const string HeadlineResource = "kleenestar.portal:admin.fields.headline";

        /// <summary>Resource key for the empty-state message.</summary>
        public const string EmptyResource = "kleenestar.portal:admin.fields.empty";

        /// <summary>Resource key for the name column.</summary>
        public const string NameColumnResource = "kleenestar.portal:admin.fields.column.name";

        /// <summary>Resource key for the type column.</summary>
        public const string TypeColumnResource = "kleenestar.portal:admin.fields.column.type";

        /// <summary>Resource key for the cardinality column.</summary>
        public const string CardinalityColumnResource = "kleenestar.portal:admin.fields.column.cardinality";

        /// <summary>Resource key for the required column.</summary>
        public const string RequiredColumnResource = "kleenestar.portal:admin.fields.column.required";

        /// <summary>Resource key for the unique column.</summary>
        public const string UniqueColumnResource = "kleenestar.portal:admin.fields.column.unique";

        /// <summary>Resource key for the description column.</summary>
        public const string DescriptionColumnResource = "kleenestar.portal:admin.fields.column.description";

        /// <summary>Resource key for the delete confirmation; takes the field name.</summary>
        public const string DeleteConfirmResource = "kleenestar.portal:admin.fields.action.delete-confirm";

        /// <summary>Resource key for the actions column.</summary>
        public const string ActionsColumnResource = "kleenestar.portal:admin.fields.column.actions";

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
            if (classIdParameter is null || !Guid.TryParse(classIdParameter.Value, out var classId))
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

            var cls = _portalManager.GetClass(classId);
            if (cls is null)
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

            // see the classes index: the key has to be resolved before it is prefixed
            visualTree.Title = $"{cls.Name} — {I18N.Translate(renderContext, HeadlineResource)}";
            visualTree.Content.MainPanel.Headline.Title = HeadlineResource;

            // add-field button: opens a modal that posts to the create REST endpoint.
            var createUri = PortalHub
                .GetUri<global::KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Fields.Create>()
                ?.BindParameters(new ClassIdParameter(classId.ToString()));
            var addForm = new ControlModalForm("portal-field-add-form")
            {
                Uri = _ => createUri,
                Method = _ => WebExpress.WebCore.WebMessage.RequestMethod.POST
            };
            addForm.Add(new ControlFormItemInputText("name")
            {
                Label = _ => "kleenestar.portal:admin.fields.column.name",
                Required = _ => true
            });
            addForm.Add(new ControlFormItemInputText("description")
            {
                Label = _ => "kleenestar.portal:admin.fields.column.description",
                Required = _ => false
            });
            addForm.AddPrimaryButton(new ControlFormItemButtonSubmit() { Text = _ => "kleenestar.portal:admin.fields.action.add" });
            visualTree.Content.MainPanel.AddPrimary(addForm);

            var fields = _portalManager.GetFields(classId);
            if (fields.Count == 0)
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
                .AddColumn(TypeColumnResource)
                .AddColumn(CardinalityColumnResource)
                .AddColumn(RequiredColumnResource)
                .AddColumn(UniqueColumnResource)
                .AddColumn(DescriptionColumnResource)
                .AddColumn(ActionsColumnResource);

            var classIdParam = new ClassIdParameter(classId.ToString());

            foreach (var field in fields)
            {
                var capturedId = field.Id;
                var capturedName = field.Name;
                var capturedDescription = field.Description ?? string.Empty;

                // row actions: edit (PUT), clone (POST), delete (DELETE)
                var updateUri = PortalHub
                    .GetUri<global::KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Fields._fieldid_.Update>()
                    ?.BindParameters(classIdParam)
                    ?.BindParameters(new FieldIdParameter(capturedId.ToString()));

                var cloneUri = PortalHub
                    .GetUri<global::KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Fields._fieldid_.Clone>()
                    ?.BindParameters(classIdParam)
                    ?.BindParameters(new FieldIdParameter(capturedId.ToString()));

                var deleteUri = PortalHub
                    .GetUri<global::KleeneStar.Portal.WWW.Api._1_.Classes._classid_.Fields._fieldid_.Delete>()
                    ?.BindParameters(classIdParam)
                    ?.BindParameters(new FieldIdParameter(capturedId.ToString()));

                var editForm = new ControlModalForm($"portal-field-edit-form-{capturedId}")
                {
                    Uri = _ => updateUri,
                    Method = _ => WebExpress.WebCore.WebMessage.RequestMethod.PUT
                };
                editForm.Add(new ControlFormItemInputText("name")
                {
                    Label = _ => "kleenestar.portal:admin.fields.column.name",
                    Required = _ => true
                });
                editForm.Add(new ControlFormItemInputText("description")
                {
                    Label = _ => "kleenestar.portal:admin.fields.column.description"
                });
                editForm.AddPrimaryButton(new ControlFormItemButtonSubmit() { Text = _ => "kleenestar.portal:admin.fields.action.save" });

                var cloneForm = new ControlModalForm($"portal-field-clone-form-{capturedId}")
                {
                    Uri = _ => cloneUri,
                    Method = _ => WebExpress.WebCore.WebMessage.RequestMethod.POST
                };
                cloneForm.AddPrimaryButton(new ControlFormItemButtonSubmit() { Text = _ => "kleenestar.portal:admin.fields.action.clone" });

                var deleteForm = new ControlModalFormConfirmDelete($"portal-field-delete-form-{capturedId}")
                {
                    Uri = _ => deleteUri,
                    Method = _ => WebExpress.WebCore.WebMessage.RequestMethod.DELETE
                };
                var deleteConfirmation = I18N.Translate(renderContext, DeleteConfirmResource, capturedName);
                deleteForm.Conformation = _ => new ControlText() { Text = _ => deleteConfirmation };

                var descriptionText = new ControlText()
                {
                    Text = _ => capturedDescription,
                    TextColor = _ => new PropertyColorText(TypeColorText.Secondary)
                };
                var descriptionCell = new ControlTableCellPanel();
                descriptionCell.Add(new IControl[] { descriptionText });

                var actionsCell = new ControlTableCellPanel();
                actionsCell.Add(new IControl[] { editForm, cloneForm, deleteForm });

                table = table.AddRow
                (
                    new ControlTableCell() { Text = _ => capturedName },
                    new ControlTableCell() { Text = _ => field.FieldType.ToString() },
                    new ControlTableCell() { Text = _ => field.Cardinality.ToString() },
                    new ControlTableCell() { Text = _ => field.Required ? "✓" : "—" },
                    new ControlTableCell() { Text = _ => field.Unique ? "✓" : "—" },
                    descriptionCell,
                    actionsCell
                );
            }

            visualTree.Content.MainPanel.AddPrimary(table);
        }
    }
}
