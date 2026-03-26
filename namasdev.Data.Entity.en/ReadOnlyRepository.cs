using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

        public TEntity Get(TId id,
            IEnumerable<string> loadProperties,
            bool includeDeleted = false)
        {
            using (var ctx = new TDbContext())
            {
                var query = ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id));

                query = FilterDeleted(query, includeDeleted);

                return query.FirstOrDefault();
            }
        }

        public TEntity Get(TId id,
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false)
        {
            using (var ctx = new TDbContext())
            {
                var query = ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id));

                query = FilterDeleted(query, includeDeleted);

                return query.FirstOrDefault();
            }
        }

        public TEntity Get(TId id,
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false)
        {
            using (var ctx = new TDbContext())
            {
                var query = ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties)
                    .Where(e => e.Id.Equals(id));

                query = FilterDeleted(query, includeDeleted);

                return query.FirstOrDefault();
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

        public IEnumerable<TEntity> GetList(
            IEnumerable<string> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            using (var ctx = new TDbContext())
            {
                var query = ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties);

                query = FilterDeleted(query, includeDeleted);

                return query
                    .OrderAndPage(op)
                    .ToArray();
            }
        }

        public IEnumerable<TEntity> GetList(
            IEnumerable<Expression<Func<TEntity, object>>> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            using (var ctx = new TDbContext())
            {
                var query = ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties);

                query = FilterDeleted(query, includeDeleted);

                return query
                    .OrderAndPage(op)
                    .ToArray();
            }
        }

        public IEnumerable<TEntity> GetList(
            ILoadProperties<TEntity> loadProperties,
            bool includeDeleted = false,
            OrderAndPagingParameters op = null)
        {
            using (var ctx = new TDbContext())
            {
                var query = ctx.Set<TEntity>()
                    .IncludeMultiple(loadProperties);

                query = FilterDeleted(query, includeDeleted);

                return query
                    .OrderAndPage(op)
                    .ToArray();
            }
        }

        public bool ExistsById(TId id,
            bool includeDeleted = false)
        {
            using (var ctx = new TDbContext())
            {
                var query = ctx.Set<TEntity>()
                    .Where(e => e.Id.Equals(id));

                query = FilterDeleted(query, includeDeleted);

                return query.Any();
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
