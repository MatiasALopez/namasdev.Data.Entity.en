using Xunit;

namespace namasdev.Data.Entity.Tests.Helpers
{
    // Shares one LocalDbFixture across all test classes in this collection and forces
    // them to run serially, so the two classes don't drop/recreate the same LocalDB
    // database concurrently.
    [CollectionDefinition(Name)]
    public class LocalDbCollection : ICollectionFixture<LocalDbFixture>
    {
        public const string Name = "LocalDb";
    }
}
