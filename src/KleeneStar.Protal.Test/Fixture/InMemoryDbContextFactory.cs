using KleeneStar.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;

namespace KleeneStar.Portal.Test
{
    /// <summary>
    /// Provides a factory for creating an in-memory KleeneStarDbContext instance.
    /// The public static <see cref="Create"/> method doubles as the reflection-loaded
    /// database provider entry point used by <c>ModelHub.CreateDbContext</c> when the
    /// database config points at this test assembly.
    /// </summary>
    public static class InMemoryDbContextFactory
    {
        private static readonly ConcurrentDictionary<string, DbContextOptions<KleeneStarDbContext>> _optionsCache
            = new();

        /// <summary>
        /// Creates an in-memory DbContext using the given database name.
        /// </summary>
        /// <param name="connectionString">The in-memory database name.</param>
        /// <returns>A configured KleeneStarDbContext instance.</returns>
        public static KleeneStarDbContext Create(string connectionString)
        {
            var key = string.IsNullOrWhiteSpace(connectionString)
                ? "DefaultInMemoryDb"
                : connectionString;

            var options = _optionsCache.GetOrAdd(key, _ =>
            {
                return new DbContextOptionsBuilder<KleeneStarDbContext>()
                    .UseInMemoryDatabase(key)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .Options;
            });

            return new KleeneStarDbContext(options);
        }
    }
}
