namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// A pre-bound form that lets a customer create an issue without choosing every field
    /// from scratch. Templates carry a title, a short description, and (after selection)
    /// prefilled defaults for the underlying <c>Form</c>.
    /// </summary>
    public interface ITemplate
    {
        /// <summary>
        /// Gets the stable key of the template — used in URIs.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets the human-readable title of the template.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets a short description shown beneath the title.
        /// </summary>
        string Description { get; }
    }
}
