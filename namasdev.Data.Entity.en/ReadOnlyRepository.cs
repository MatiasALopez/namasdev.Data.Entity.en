using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using namasdev.Core.Entity;
using namasdev.Core.Linq;
using namasdev.Core.Reflection;

namespace namasdev.Data.Entity
{
    public class ReadOnlyRepository<TDbContext, TEntity, TId> : IReadOnlyRepository<TEntity, TId>
        where TDbContext : DbContextBase, new()
        where TEntity : class, IEntity<TId>, new()
        where TId : IEquatable<TId>
    {
        private static readonly Expression<Func<TEntity, bool>> _notDeletedPredicate =
            ReflectionHelper.ClassImplementsInterface<TEntity, IEntityDeleted>()
            ? BuildNotDeletedPredicate()
            : null;

        private static Expression<Func<TEntity, bool>> BuildNotDeletedPredicate()
        {
            var param = Expression.Parameter(typeof(TEntity), "e");
            var notDeleted = Expression.Not(Expression.Property(param, nameof(IEntityDeleted.Deleted)));
            return Expression.Lambda<Func<TEntity, bool>>(notDeleted, param);
        }

        public TEntity Get(TId id)
        {
            return Get(id, 
                includeDeleted: false);
        }

        public TEntity Get(TId id, bool includeDeleted)
        {
            return Get(id,
                loadProperties: (IEnumerable<string>)null,
                includeDeleted: includeDeleted);
        }

        public Task<TEntity> GetAsync(TId id,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            return GetAsync(id,
                loadProperties: (IEnumerable<string>)null,
                includeDeleted: includeDeleted,
                ct: ct);
        }

        public TEntity Get(TId id,
            IEnumerable<string> loadProperties,
            bool includeDeleted = false)
        {
            using (var ctx = new TDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .FirstOrDefault();
            }
        }

        public async Task<TEntity> GetAsync(TId id,
            IEnumerable<string> loadProperties,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            using (var ctx = new TDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .FirstOrDefaultAsync(ct);
            }
        }

        public TEntity Get(TId id,
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false)
        {
            using (var ctx = new TDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .FirstOrDefault();
            }
        }

        public async Task<TEntity> GetAsync(TId id,
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            using (var ctx = new TDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .FirstOrDefaultAsync(ct);
            }
        }

        public TEntity Get(TId id,
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false)
        {
            using (var ctx = new TDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .FirstOrDefault();
            }
        }

        public async Task<TEntity> GetAsync(TId id,
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            using (var ctx = new TDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id))
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .FirstOrDefaultAsync(ct);
            }
        }

        public IEnumerable<TEntity> GetList(
            OrderAndPagingParameters op = null)
        {
            return GetList(
                includeDeleted: false,
                op);
        }

        public IEnumerable<TEntity> GetList(
            bool includeDeleted,
            OrderAndPagingParameters op = null)
        {
            return GetList(
                includeDeleted: includeDeleted,
                op: op,
                loadProperties: (IEnumerable<string>)null);
        }

        public Task<IEnumerable<TEntity>> GetListAsync(
            bool includeDeleted = false,
            OrderAndPagingParameters op = null,
            CancellationToken ct = default)
        {
            return GetListAsync(
                loadProperties: (IEnumerable<string>)null,
                includeDeleted: includeDeleted,
                op: op,
                ct: ct);
        }

        public IEnumerable<TEntity> GetList(
            IEnumerable<string> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            using (var ctx = new TDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .OrderAndPage(op)
                    .ToArray();
            }
        }

        public async Task<IEnumerable<TEntity>> GetListAsync(
            IEnumerable<string> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null,
            CancellationToken ct = default)
        {
            using (var ctx = new TDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .OrderAndPage(op)
                    .ToArrayAsync(ct);
            }
        }

        public IEnumerable<TEntity> GetList(
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            using (var ctx = new TDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .OrderAndPage(op)
                    .ToArray();
            }
        }

        public async Task<IEnumerable<TEntity>> GetListAsync(
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null,
            CancellationToken ct = default)
        {
            using (var ctx = new TDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .OrderAndPage(op)
                    .ToArrayAsync(ct);
            }
        }

        public IEnumerable<TEntity> GetList(
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            using (var ctx = new TDbContext())
            {
                return ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .OrderAndPage(op)
                    .ToArray();
            }
        }

        public async Task<IEnumerable<TEntity>> GetListAsync(
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null,
            CancellationToken ct = default)
        {
            using (var ctx = new TDbContext())
            {
                return await ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .OrderAndPage(op)
                    .ToArrayAsync(ct);
            }
        }

        public bool ExistsById(TId id,
            bool includeDeleted = false)
        {
            using (var ctx = new TDbContext())
            {
                return ctx.Set<TEntity>()
                    .Where(e => e.Id.Equals(id))
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .Any();
            }
        }

        public async Task<bool> ExistsByIdAsync(TId id,
            bool includeDeleted = false,
            CancellationToken ct = default)
        {
            using (var ctx = new TDbContext())
            {
                return await ctx.Set<TEntity>()
                    .Where(e => e.Id.Equals(id))
                    .Apply(query => FilterDeleted(query, includeDeleted))
                    .AnyAsync(ct);
            }
        }

        private IQueryable<TEntity> FilterDeleted(IQueryable<TEntity> query, bool includeDeleted)
        {
            return
                _notDeletedPredicate == null || includeDeleted
                ? query
                : query.Where(_notDeletedPredicate);
        }
    }
}
