namespace KleeneStar.Portal.WWW.Api._1_.Issues.Mine
{
    /// <summary>
    /// Quickfilter endpoint for the "My Issues" view. The options (the collapsed
    /// portal lifecycle states) are provided by <see cref="IssueQuickfilterBase"/>;
    /// this leaf type exists only to expose the route
    /// <c>/api/1/issues/mine/quickfilter</c>.
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
