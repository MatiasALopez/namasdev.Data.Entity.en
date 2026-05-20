using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using namasdev.Core.Linq;
using namasdev.Data.Entity.Tests.Helpers;

namespace namasdev.Data.Entity.Tests
{
    [Collection(LocalDbCollection.Name)]
    public class ReadOnlyRepositoryTests
    {
        private static OrderAndPagingParameters ById()
            => new OrderAndPagingParameters { Order = "Id", Page = 1, ItemsPerPage = 1000 };

        private static TestEntity MakeEntity(int id, bool deleted = false, TestCategory category = null) => new TestEntity
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
            Category = category,
            CategoryId = category?.Id,
        };

        // Clears the store and seeds, then returns a repository that opens its own contexts.
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

        // ── Get ────────────────────────────────────────────────────────────────

        [Fact]
        public void Get_ExistingId_ReturnsEntity()
        {
            var repo = Setup(MakeEntity(1));

            var result = repo.Get(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public void Get_NonExistingId_ReturnsNull()
        {
            var repo = Setup();

            Assert.Null(repo.Get(99));
        }

        [Fact]
        public void Get_DeletedEntity_ExcludedByDefault()
        {
            var repo = Setup(MakeEntity(1, deleted: true));

            Assert.Null(repo.Get(1));
        }

        [Fact]
        public void Get_DeletedEntity_ReturnedWhenIncludeDeletedTrue()
        {
            var repo = Setup(MakeEntity(1, deleted: true));

            Assert.NotNull(repo.Get(1, includeDeleted: true));
        }

        [Fact]
        public void Get_WithoutLoadProperties_DoesNotLoadNavigation()
        {
            var repo = Setup(MakeEntity(1, category: new TestCategory { Id = 10, Name = "Cat" }));

            var result = repo.Get(1);

            Assert.NotNull(result);
            Assert.Null(result.Category);
        }

        [Fact]
        public void Get_WithStringLoadProperties_LoadsNavigation()
        {
            var repo = Setup(MakeEntity(1, category: new TestCategory { Id = 10, Name = "Cat" }));

            var result = repo.Get(1, loadProperties: new[] { nameof(TestEntity.Category) });

            Assert.NotNull(result.Category);
            Assert.Equal("Cat", result.Category.Name);
        }

        [Fact]
        public void Get_WithExpressionLoadProperties_LoadsNavigation()
        {
            var repo = Setup(MakeEntity(1, category: new TestCategory { Id = 10, Name = "Cat" }));

            var result = repo.Get(1, loadProperties: new System.Linq.Expressions.Expression<Func<TestEntity, object>>[] { e => e.Category });

            Assert.NotNull(result.Category);
        }

        [Fact]
        public void Get_WithILoadProperties_LoadsNavigation()
        {
            var repo = Setup(MakeEntity(1, category: new TestCategory { Id = 10, Name = "Cat" }));

            var result = repo.Get(1, loadProperties: new CategoryLoadProperties());

            Assert.NotNull(result.Category);
        }

        [Fact]
        public async Task GetAsync_ExistingId_ReturnsEntity()
        {
            var repo = Setup(MakeEntity(1));

            var result = await repo.GetAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetAsync_DeletedEntity_ExcludedByDefault()
        {
            var repo = Setup(MakeEntity(1, deleted: true));

            Assert.Null(await repo.GetAsync(1));
        }

        [Fact]
        public async Task GetAsync_WithLoadProperties_LoadsNavigation()
        {
            var repo = Setup(MakeEntity(1, category: new TestCategory { Id = 10, Name = "Cat" }));

            var result = await repo.GetAsync(1, loadProperties: new[] { nameof(TestEntity.Category) });

            Assert.NotNull(result.Category);
        }

        // ── GetList ────────────────────────────────────────────────────────────

        [Fact]
        public void GetList_ReturnsOnlyNonDeleted()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2, deleted: true), MakeEntity(3));

            var result = repo.GetList(op: ById());

            Assert.Equal(2, result.Count());
            Assert.All(result, e => Assert.False(e.Deleted));
        }

        [Fact]
        public void GetList_IncludeDeleted_ReturnsAll()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2, deleted: true));

            Assert.Equal(2, repo.GetList(includeDeleted: true, op: ById()).Count());
        }

        [Fact]
        public void GetList_EmptyStore_ReturnsEmpty()
        {
            var repo = Setup();

            Assert.Empty(repo.GetList(op: ById()));
        }

        [Fact]
        public void GetList_Paging_ReturnsRequestedPageAndTotalCount()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2), MakeEntity(3));
            var op = new OrderAndPagingParameters { Order = "Id", Page = 2, ItemsPerPage = 1 };

            var result = repo.GetList(op: op).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Id);
            Assert.Equal(3, op.ItemsTotalCount);
        }

        [Fact]
        public async Task GetListAsync_ReturnsOnlyNonDeleted()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2, deleted: true));

            var result = await repo.GetListAsync(op: ById());

            Assert.Single(result);
        }

        [Fact]
        public async Task GetListAsync_IncludeDeleted_ReturnsAll()
        {
            var repo = Setup(MakeEntity(1), MakeEntity(2, deleted: true));

            Assert.Equal(2, (await repo.GetListAsync(includeDeleted: true, op: ById())).Count());
        }

        // ── ExistsById ─────────────────────────────────────────────────────────

        [Fact]
        public void ExistsById_ExistingId_ReturnsTrue()
        {
            var repo = Setup(MakeEntity(1));

            Assert.True(repo.ExistsById(1));
        }

        [Fact]
        public void ExistsById_NonExistingId_ReturnsFalse()
        {
            var repo = Setup();

            Assert.False(repo.ExistsById(99));
        }

        [Fact]
        public void ExistsById_DeletedEntity_ReturnsFalseByDefault()
        {
            var repo = Setup(MakeEntity(1, deleted: true));

            Assert.False(repo.ExistsById(1));
        }

        [Fact]
        public void ExistsById_DeletedEntity_ReturnsTrueWhenIncluded()
        {
            var repo = Setup(MakeEntity(1, deleted: true));

            Assert.True(repo.ExistsById(1, includeDeleted: true));
        }

        [Fact]
        public async Task ExistsByIdAsync_ExistingId_ReturnsTrue()
        {
            var repo = Setup(MakeEntity(1));

            Assert.True(await repo.ExistsByIdAsync(1));
        }

        [Fact]
        public async Task ExistsByIdAsync_NonExistingId_ReturnsFalse()
        {
            var repo = Setup();

            Assert.False(await repo.ExistsByIdAsync(99));
        }

        // ── AllPrimitiveFields preserved on round-trip ─────────────────────────

        [Fact]
        public void Get_AllPrimitiveFieldValues_RoundTrip()
        {
            var entity = MakeEntity(1);
            var repo = Setup(entity);

            var result = repo.Get(1);

            Assert.Equal(entity.IntValue, result.IntValue);
            Assert.Equal(entity.ShortValue, result.ShortValue);
            Assert.Equal(entity.LongValue, result.LongValue);
            Assert.Equal(entity.DecimalValue, result.DecimalValue);
            Assert.Equal(entity.DoubleValue, result.DoubleValue);
            Assert.Equal(entity.DateTimeValue, result.DateTimeValue);
            Assert.Equal(entity.StringValue, result.StringValue);
            Assert.Equal(entity.BoolValue, result.BoolValue);
        }
    }
}
