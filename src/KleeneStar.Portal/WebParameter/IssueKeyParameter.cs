using WebExpress.WebCore.WebParameter;

namespace KleeneStar.Portal.WebParameter
{
    /// <summary>
    /// Represents a parameter that specifies an issue key (e.g. <c>INC-2041</c>).
    /// </summary>
    public sealed class IssueKeyParameter : IParameterStatic
    {
        /// <summary>
        /// Gets the key that uniquely identifies the parameter in configuration or settings contexts.
        /// </summary>
        public static string Key => "issuekey";

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
        public IssueKeyParameter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with a specified value.
        /// </summary>
        /// <param name="value">The value of the parameter.</param>
        public IssueKeyParameter(string value)
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
