namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// Default <see cref="ITemplate"/> implementation backed by the underlying
    /// <c>Form</c> projection.
    /// </summary>
    public sealed class Template : ITemplate
    {
        /// <inheritdoc/>
        public string Key { get; init; }

        /// <inheritdoc/>
        public string Title { get; init; }

        /// <inheritdoc/>
        public string Description { get; init; }
    }
}
