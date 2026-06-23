using System.Collections.Generic;
using WebExpress.WebApp.WebRestApi;
using WebExpress.WebCore.WebMessage;

// The entity type Object collides with System.Object; alias it so the
// prompt type argument reads naturally.
using ObjectEntity = KleeneStar.Model.Entities.Object;

namespace KleeneStar.Portal.WWW.Api._1_.Issues
{
    /// <summary>
    /// Shared base for the issue-list advanced-search prompt endpoints
    /// (<c>Mine/Wql</c> and <c>Org/Wql</c>). The control fetches its history and
    /// lookahead suggestions from this endpoint while the plain-text query is
    /// forwarded to the table endpoint as the <c>q</c> parameter.
    /// </summary>
    public abstract class IssueWqlBase : RestApiWqlPrompt<ObjectEntity>
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        protected IssueWqlBase()
        {
        }

        /// <summary>
        /// Provides a small set of example queries shown in the prompt history.
        /// </summary>
        /// <param name="request">The request for which to retrieve history.</param>
        /// <returns>The example query history entries.</returns>
        protected override IEnumerable<string> GetHistory(IRequest request)
        {
            yield return "Summary ~ \"Login\"";
            yield return "Key ~ \"SD\"";
        }
    }
}
