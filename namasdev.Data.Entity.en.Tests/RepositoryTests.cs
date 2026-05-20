using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using namasdev.Data.Entity.Tests.Helpers;

namespace namasdev.Data.Entity.Tests
{
    [Collection(LocalDbCollection.Name)]
    public class RepositoryTests
    {
        // Keys are not store-generated (see TestEntity), so callers supply explicit Ids.
        private static TestEntity MakeEntity(int id, bool deleted = false) => new TestEntity
        {
            Id = id,
            IntValue = id,
            ShortValue = (short)(id % short.MaxValue),
            LongValue = (long)id * 1000,
            DecimalValue = id * 1.5m,
            DoubleValue = id * 1.5,
            DateTimeValue = new DateTime(2024, 1, 1).AddDays(id),
            StringValue = $"Entity{id}",
            BoolValue = id % 2 == 0,
            CreatedBy = "seed",
            CreatedAt = new DateTime(2024, 1, 1),
            Deleted = deleted,
            DeletedBy = deleted ? "seed" : null,
            DeletedAt = deleted ? new DateTime(2024, 1, 1) : (DateTime?)null,
        };

        private static TestRepository Setup(params TestEntity[] seed)
        {
            using (var ctx = new TestDbContext())
            {
                ctx.Database.ExecuteSqlCommand("DELETE FROM TestEntities");
                ctx.Database.ExecuteSqlCommand("DELETE FROM TestCategories");

                if (seed.Length > 0)
                {
                    ctx.TestEntities.AddRange(seed);
                    ctx.SaveChanges();
                }
            }

            return new TestRepository();
        }

        private static int Count(bool includeDeleted = false)
        {
            using (var ctx = new TestDbContext())
            {
                return includeDeleted
                    ? ctx.TestEntities.Count()
                    : ctx.TestEntities.Count(e => !e.Deleted);
            }
        }

        // ── Add ────────────────────────────────────────────────────────────────

        [Fact]
        public void Add_Entity_IsPersisted()
        {
            var repo = Setup();

            repo.Add(MakeEntity(1));

            Assert.NotNull(repo.Get(1));
        }

        [Fact]
        public async Task AddAsync_Entity_IsPersisted()
        {
            var repo = Setup();

            await repo.AddAsync(MakeEntity(1));

            Assert.NotNull(repo.Get(1));
        }

        [Fact]
        public void Add_Batch_AllEntitiesPersisted()
        {
            var repo = Setup();
            var entities = Enumerable.Range(1, 5).Select(i => MakeEntity(i)).ToList();

            repo.Add(entities);

            Assert.Equal(5, Count());
        }

        [Fact]
        public void Add_BatchSpanningMultipleBatches_AllEntitiesPersisted()
        {
            var repo = Setup();
            var entities = Enumerable.Range(1, 5).Select(i => MakeEntity(i)).ToList();

            repo.Add(entities, batchSize: 2);

            Assert.Equal(5, Count());
        }

        [Fact]
        public async Task AddAsync_Batch_AllEntitiesPersisted()
        {
            var repo = Setup();
            var entities = Enumerable.Range(1, 3).Select(i => MakeEntity(i)).ToList();

            await repo.AddAsync(entities);

            Assert.Equal(3, Count());
        }

        // ── Update ─────────────────────────────────────────────────────────────

        [Fact]
        public void Update_ChangesArePersisted()
        {
            var repo = Setup(MakeEntity(1));

            var entity = repo.Get(1);
            entity.StringValue = "updated";
            repo.Update(entity);

            Assert.Equal("updated", repo.Get(1).StringValue);
        }

        [Fact]
        public void Update_ExcludesCreatedProperties()
        {
            var seed = MakeEntity(1);
            seed.CreatedBy = "original";
            var repo = Setup(seed);

            // Full-entity update (SQL Server rejects a sparse stub's default DateTime),
            // tampering with the audit columns that Update is expected to ignore.
            var entity = MakeEntity(1);
            entity.StringValue = "changed";
            entity.CreatedBy = "tampered";
            entity.CreatedAt = new DateTime(1999, 1, 1);
            repo.Update(entity);

            var result = repo.Get(1);
            Assert.Equal("changed", result.StringValue);
            Assert.Equal("original", result.CreatedBy);
        }

        [Fact]
        public async Task UpdateAsync_ChangesArePersisted()
        {
            var repo = Setup(MakeEntity(1));

            var entity = repo.Get(1);
            entity.StringValue = "async-updated";
            await repo.UpdateAsync(entity);

            Assert.Equal("async-updated", repo.Get(1).StringValue);
        }

        [Fact]
        public void Update_Batch_AllEntitiesUpdated()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2));

            var e1 = repo.Get(1); var e2 = repo.Get(2);
            e1.StringValue = "A"; e2.StringValue = "B";
            repo.Update(new[] { e1, e2 });

            Assert.Equal("A", repo.Get(1).StringValue);
            Assert.Equal("B", repo.Get(2).StringValue);
        }

        // ── UpdateProperties ───────────────────────────────────────────────────

        [Fact]
        public void UpdateProperties_OnlyNamedPropertyIsChanged()
        {
            var repo = Setup(MakeEntity(1));

            var stub = new TestEntity { Id = 1, StringValue = "changed", IntValue = 999 };
            repo.UpdateProperties(stub, nameof(TestEntity.StringValue));

            var result = repo.Get(1);
            Assert.Equal("changed", result.StringValue);
            Assert.Equal(1, result.IntValue);
        }

        [Fact]
        public async Task UpdatePropertiesAsync_OnlyNamedPropertyIsChanged()
        {
            var repo = Setup(MakeEntity(1));

            var stub = new TestEntity { Id = 1, StringValue = "async-changed", IntValue = 999 };
            await repo.UpdatePropertiesAsync(stub, new[] { nameof(TestEntity.StringValue) });

            var result = repo.Get(1);
            Assert.Equal("async-changed", result.StringValue);
            Assert.Equal(1, result.IntValue);
        }

        [Fact]
        public void UpdateProperties_Batch_UpdatesAllEntities()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2));

            var s1 = new TestEntity { Id = 1, StringValue = "upd1" };
            var s2 = new TestEntity { Id = 2, StringValue = "upd2" };
            repo.UpdateProperties(new[] { s1, s2 }, properties: nameof(TestEntity.StringValue));

            Assert.Equal("upd1", repo.Get(1).StringValue);
            Assert.Equal("upd2", repo.Get(2).StringValue);
        }

        // ── UpdateDeletedProperties ────────────────────────────────────────────

        [Fact]
        public void UpdateDeletedProperties_UpdatesDeletedByAndDeletedAt()
        {
            var repo = Setup(MakeEntity(1));

            var deletedAt = new DateTime(2024, 6, 1);
            var stub = new TestEntity { Id = 1, DeletedBy = "admin", DeletedAt = deletedAt };
            repo.UpdateDeletedProperties(stub);

            var result = repo.Get(1);
            Assert.Equal("admin", result.DeletedBy);
            Assert.Equal(deletedAt, result.DeletedAt);
        }

        [Fact]
        public async Task UpdateDeletedPropertiesAsync_UpdatesDeletedByAndDeletedAt()
        {
            var repo = Setup(MakeEntity(1));

            var deletedAt = new DateTime(2024, 6, 1);
            var stub = new TestEntity { Id = 1, DeletedBy = "admin", DeletedAt = deletedAt };
            await repo.UpdateDeletedPropertiesAsync(stub);

            var result = repo.Get(1);
            Assert.Equal("admin", result.DeletedBy);
            Assert.Equal(deletedAt, result.DeletedAt);
        }

        // ── Soft delete via Update ─────────────────────────────────────────────

        [Fact]
        public void Update_CanSetDeletedFlag()
        {
            var repo = Setup(MakeEntity(1));

            var entity = repo.Get(1);
            entity.Deleted = true;
            repo.Update(entity);

            Assert.Null(repo.Get(1));
            Assert.NotNull(repo.Get(1, includeDeleted: true));
        }

        // ── Delete ─────────────────────────────────────────────────────────────

        [Fact]
        public void Delete_Entity_IsRemoved()
        {
            var repo = Setup(MakeEntity(1));

            repo.Delete(repo.Get(1));

            Assert.False(repo.ExistsById(1, includeDeleted: true));
        }

        [Fact]
        public async Task DeleteAsync_Entity_IsRemoved()
        {
            var repo = Setup(MakeEntity(1));

            await repo.DeleteAsync(repo.Get(1));

            Assert.False(repo.ExistsById(1, includeDeleted: true));
        }

        [Fact]
        public void Delete_Batch_AllEntitiesRemoved()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2), MakeEntity(3));

            repo.Delete(new[] { repo.Get(1), repo.Get(2) });

            Assert.Equal(1, Count(includeDeleted: true));
            Assert.True(repo.ExistsById(3));
        }

        // ── DeleteById ─────────────────────────────────────────────────────────

        [Fact]
        public void DeleteById_EntityIsRemoved()
        {
            var repo = Setup(MakeEntity(1));

            repo.DeleteById(1);

            Assert.False(repo.ExistsById(1, includeDeleted: true));
        }

        [Fact]
        public async Task DeleteByIdAsync_EntityIsRemoved()
        {
            var repo = Setup(MakeEntity(1));

            await repo.DeleteByIdAsync(1);

            Assert.False(repo.ExistsById(1, includeDeleted: true));
        }

        // ── DeleteByIds ────────────────────────────────────────────────────────

        [Fact]
        public void DeleteByIds_OnlyTargetedEntitiesRemoved()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2), MakeEntity(3));

            repo.DeleteByIds(new[] { 1, 2 });

            Assert.Equal(1, Count(includeDeleted: true));
            Assert.True(repo.ExistsById(3));
        }

        [Fact]
        public async Task DeleteByIdsAsync_OnlyTargetedEntitiesRemoved()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2));

            await repo.DeleteByIdsAsync(new[] { 1 });

            Assert.Equal(1, Count(includeDeleted: true));
        }
    }
}
