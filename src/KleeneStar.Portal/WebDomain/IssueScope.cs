namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// The scope used to filter issue queries — <c>Mine</c> shows only issues the calling
    /// identity created or was added to; <c>Organization</c> shows everything inside the
    /// active tenant the active permission profile permits.
    /// </summary>
    public enum IssueScope
    {
        /// <summary>
        /// Issues the calling identity created, was shared with, or is watching.
        /// </summary>
        Mine,

        /// <summary>
        /// All issues inside the calling identity's tenant the active profile permits.
        /// </summary>
        Organization
    }
}
