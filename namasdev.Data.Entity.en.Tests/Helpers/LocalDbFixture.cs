using System;
using System.Data.Entity;

namespace namasdev.Data.Entity.Tests.Helpers
{
    // Creates a fresh LocalDB database (schema from the Code First model) for the
    // duration of a test class, then drops it. The initializer is disabled so the
    // repository's internal `new TDbContext()` calls don't trigger EF's default
    // create/migrate behavior against the already-provisioned schema.
    public class LocalDbFixture : IDisposable
    {
        public LocalDbFixture()
        {
            Database.SetInitializer<TestDbContext>(null);

            using (var ctx = new TestDbContext())
            {
                if (ctx.Database.Exists())
                {
                    ctx.Database.Delete();
                }

                ctx.Database.Create();
            }
        }

        public void Dispose()
        {
            using (var ctx = new TestDbContext())
            {
                if (ctx.Database.Exists())
                {
                    ctx.Database.Delete();
                }
            }
        }
    }
}
