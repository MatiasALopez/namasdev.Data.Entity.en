namespace namasdev.Data.Entity.Tests.Helpers
{
    // Extends the write-capable Repository so both ReadOnlyRepository and Repository
    // tests can share one helper (Repository inherits all read methods).
    public class TestRepository : Repository<TestDbContext, TestEntity, int>
    {
    }
}
