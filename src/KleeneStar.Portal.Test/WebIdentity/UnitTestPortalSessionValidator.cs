using KleeneStar.Core.WebIdentity;
using KleeneStar.Portal.WebIdentity;

namespace KleeneStar.Portal.Test.WebIdentity
{
    /// <summary>
    /// Tests the identity validator of the portal application.
    /// </summary>
    public class UnitTestPortalSessionValidator
    {
        /// <summary>
        /// The framework discovers the validator in the portal assembly only when it is public
        /// and sealed, and it judges like the core's.
        /// </summary>
        [Fact]
        public void ValidatorIsDiscoverable()
        {
            var type = typeof(PortalSessionValidator);

            Assert.True(type.IsPublic && type.IsSealed);
            Assert.True(typeof(SessionValidatorBase).IsAssignableFrom(type));
            Assert.NotNull(type.GetConstructor(Type.EmptyTypes));
        }
    }
}
