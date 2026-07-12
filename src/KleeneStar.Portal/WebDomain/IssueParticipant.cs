namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// A lightweight identity projection used by the portal for requesters, sharers,
    /// watchers, and comment authors. The underlying record is an <c>Identity</c> in the
    /// shared identity model; the portal exposes only the fields it needs to render.
    /// </summary>
    public sealed class IssueParticipant
    {
        /// <summary>
        /// Gets or sets the identity id of the participant.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the participant's display name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the participant's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets the two-letter monogram derived from <see cref="Name"/> for avatar display.
        /// </summary>
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    return "?";
                }
                var parts = Name.Split(' ', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
                if (parts.Length == 1)
                {
                    return parts[0][..System.Math.Min(2, parts[0].Length)].ToUpperInvariant();
                }
                return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            }
        }
    }
}
