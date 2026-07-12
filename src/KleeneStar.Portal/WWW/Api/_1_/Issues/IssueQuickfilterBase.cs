using System.Collections.Generic;
using WebExpress.WebApp.WebRestApi;
using WebExpress.WebCore.Internationalization;
using WebExpress.WebCore.WebMessage;
using WebExpress.WebIndex.Queries;

// The entity type Object collides with System.Object; alias it so the
// quickfilter type argument reads naturally.
using ObjectEntity = KleeneStar.Model.Entities.Object;

namespace KleeneStar.Portal.WWW.Api._1_.Issues
{
    /// <summary>
    /// Shared base for the issue-list quickfilter endpoints (<c>Mine/Quickfilter</c>
    /// and <c>Org/Quickfilter</c>). Offers the collapsed portal lifecycle states
    /// (Open, In Progress, Waiting, Resolved, Closed) as toggleable chips; the issue
    /// table projection (<see cref="IssueTableProjection"/>) translates the selected
    /// ids back to states.
    /// </summary>
    public abstract class IssueQuickfilterBase : RestApiQuickfilter<ObjectEntity>
    {
        /// <summary>
        /// The quickfilter id prefix shared by every issue-state chip.
        /// </summary>
        public const string IdPrefix = "qf_";

        /// <summary>Quickfilter id of the <c>Open</c> state.</summary>
        public const string OpenId = IdPrefix + "open";

        /// <summary>Quickfilter id of the <c>In Progress</c> state.</summary>
        public const string InProgressId = IdPrefix + "inprogress";

        /// <summary>Quickfilter id of the <c>Waiting on Requester</c> state.</summary>
        public const string WaitingId = IdPrefix + "waiting";

        /// <summary>Quickfilter id of the <c>Resolved</c> state.</summary>
        public const string ResolvedId = IdPrefix + "resolved";

        /// <summary>Quickfilter id of the <c>Closed</c> state.</summary>
        public const string ClosedId = IdPrefix + "closed";

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        protected IssueQuickfilterBase()
        {
        }

        /// <summary>
        /// Retrieves the issue-state quickfilter options.
        /// </summary>
        /// <param name="context">The query context (unused — the options are static).</param>
        /// <param name="request">The request that provides the operational context.</param>
        /// <returns>The collapsed portal lifecycle states as quickfilter items.</returns>
        protected override IEnumerable<RestApiQuickfilterItem> RetrieveItems(IQueryContext context, IRequest request)
        {
            yield return new RestApiQuickfilterItem()
            {
                Id = OpenId,
                Name = I18N.Translate(request, "kleenestar.portal:state.open")
            };

            yield return new RestApiQuickfilterItem()
            {
                Id = InProgressId,
                Name = I18N.Translate(request, "kleenestar.portal:state.in-progress")
            };

            yield return new RestApiQuickfilterItem()
            {
                Id = WaitingId,
                Name = I18N.Translate(request, "kleenestar.portal:state.waiting")
            };

            yield return new RestApiQuickfilterItem()
            {
                Id = ResolvedId,
                Name = I18N.Translate(request, "kleenestar.portal:state.resolved")
            };

            yield return new RestApiQuickfilterItem()
            {
                Id = ClosedId,
                Name = I18N.Translate(request, "kleenestar.portal:state.closed")
            };
        }
    }
}
