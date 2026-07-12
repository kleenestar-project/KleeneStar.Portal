namespace KleeneStar.Portal.WWW.Api._1_.Issues.Org
{
    /// <summary>
    /// Quickfilter endpoint for the "Organization" view. The options (the collapsed
    /// portal lifecycle states) are provided by <see cref="IssueQuickfilterBase"/>;
    /// this leaf type exists only to expose the route
    /// <c>/api/1/issues/org/quickfilter</c>.
    /// </summary>
    public sealed class Quickfilter : IssueQuickfilterBase
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public Quickfilter()
        {
        }
    }
}
