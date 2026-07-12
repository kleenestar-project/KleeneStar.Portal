using System.Collections.Generic;
using WebExpress.WebCore.WebIcon;

namespace KleeneStar.Portal.WebDomain
{
    /// <summary>
    /// A pre-curated entry point into the issue catalog (e.g. "report incident",
    /// "request access"). Each request type is backed by a <c>Class</c> in the core data
    /// model that has been declared <c>PortalVisible</c>; the templates surface the forms
    /// flagged <c>PortalTemplate</c>.
    /// </summary>
    public interface IRequestType
    {
        /// <summary>
        /// Gets the stable key of the request type — used in URIs and identification.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets the human-readable title shown on the request-type tile.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets a short description shown beneath the title on the request-type tile.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the icon glyph rendered on the tile. Sourced directly from the
        /// underlying <c>Class.Icon</c> entity; <see langword="null"/> when the
        /// class has no icon set.
        /// </summary>
        IIcon Icon { get; }

        /// <summary>
        /// Gets the templates available beneath this request type.
        /// </summary>
        IReadOnlyList<ITemplate> Templates { get; }
    }
}
