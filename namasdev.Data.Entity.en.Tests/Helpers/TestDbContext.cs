using System.Data.Entity;

namespace namasdev.Data.Entity.Tests.Helpers
{
    public class TestDbContext : DbContextBase
    {
        public const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=namasdev_Data_Entity_en_Tests;Trusted_Connection=True;MultipleActiveResultSets=True;Connection Timeout=60;";

        // Parameterless ctor required by ReadOnlyRepository's `where TDbContext : new()` constraint.
        public TestDbContext()
            : base(ConnectionString)
        {
        }

        public DbSet<TestEntity> TestEntities { get; set; }
        public DbSet<TestCategory> TestCategories { get; set; }
    }
}
