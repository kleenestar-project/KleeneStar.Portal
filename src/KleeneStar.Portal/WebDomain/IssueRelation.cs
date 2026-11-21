namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// The portal-visible projection of an <c>ObjectRelation</c>: one link between the
    /// issue being read and another issue, told from the issue's own side.
    /// </summary>
    /// <remarks>
    /// Only the far end matters to a requester - which issue, what it is called and where
    /// it stands - so the projection carries that end and the caption of the relation as
    /// read from this issue (<c>blocks</c> on the source, <c>is blocked by</c> on the
    /// target). A relation whose far end is not a portal issue the caller may open is not
    /// projected at all; see <see cref="WebManager.PortalManager"/>.
    /// </remarks>
    public sealed class IssueRelation
    {
        /// <summary>
        /// Gets the id of the underlying relation.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// Gets the key of the relation type (e.g. <c>blocks</c>).
        /// </summary>
        public string TypeKey { get; init; }

        /// <summary>
        /// Gets the caption of the relation as read from this issue. Either an i18n key
        /// (the seeded catalog) or the text an administrator typed; the page resolves it.
        /// </summary>
        public string Label { get; init; }

        /// <summary>
        /// Gets a value indicating whether this issue is the source of the relation.
        /// </summary>
        public bool IsOutgoing { get; init; }

        /// <summary>
        /// Gets the key of the related issue.
        /// </summary>
        public string Key { get; init; }

        /// <summary>
        /// Gets the title of the related issue.
        /// </summary>
        public string Title { get; init; }

        /// <summary>
        /// Gets the collapsed portal state of the related issue.
        /// </summary>
        public PortalIssueState PortalState { get; init; }
    }
}
