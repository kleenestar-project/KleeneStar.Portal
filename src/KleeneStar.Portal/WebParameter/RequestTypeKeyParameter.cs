using WebExpress.WebCore.WebParameter;

namespace KleeneStar.Portal.WebParameter
{
    /// <summary>
    /// Represents a parameter that specifies a request-type key (e.g. <c>report-incident</c>).
    /// The value is a URL-safe slug derived from the request type's display name
    /// (<see cref="KleeneStar.Portal.WebManager.PortalManager"/> builds slugs the same way).
    /// </summary>
    public sealed class RequestTypeKeyParameter : IParameterStatic
    {
        /// <summary>
        /// Gets the key that uniquely identifies the parameter in configuration or settings contexts.
        /// </summary>
        public static string Key => "requesttypekey";

        /// <summary>
        /// Gets or sets the scope of the parameter.
        /// </summary>
        public ParameterScope Scope { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public RequestTypeKeyParameter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with a specified value.
        /// </summary>
        /// <param name="value">The parameter value.</param>
        public RequestTypeKeyParameter(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Retrieves the unique key associated with the current instance.
        /// </summary>
        /// <returns>The parameter key.</returns>
        public string GetKey()
        {
            return Key;
        }
    }
}
