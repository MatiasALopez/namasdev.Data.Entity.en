using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using namasdev.Core.Entity;
using namasdev.Core.Reflection;
using namasdev.Core.Validation;

namespace namasdev.Data.Entity
{
    public class Repository<TDbContext, TEntity, TId> : ReadOnlyRepository<TDbContext, TEntity, TId>, IRepository<TEntity, TId>
        where TDbContext : DbContextBase, new()
        where TEntity : class, IEntity<TId>, new()
        where TId : IEquatable<TId>
    {
        private const byte BATCH_SIZE_DEFAULT = 100;

        public virtual void Add(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            DbContextHelper<TDbContext>.AddBatch(entities,
                batchSize: batchSize);
        }

        public virtual Task AddAsync(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            return DbContextHelper<TDbContext>.AddBatchAsync(entities,
                batchSize: batchSize, ct: ct);
        }

        public virtual void Add(TEntity entity)
        {
            DbContextHelper<TDbContext>.Add(entity);
        }

        public virtual Task AddAsync(TEntity entity,
            CancellationToken ct = default)
        {
            return DbContextHelper<TDbContext>.AddAsync(entity, ct: ct);
        }

        public virtual void Update(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            DbContextHelper<TDbContext>.UpdateBatch(entities,
                batchSize: batchSize);
        }

        public virtual Task UpdateAsync(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            return DbContextHelper<TDbContext>.UpdateBatchAsync(entities,
                batchSize: batchSize, ct: ct);
        }

        public virtual void Update(TEntity entity)
        {
            DbContextHelper<TDbContext>.Update(entity);
        }

        public virtual Task UpdateAsync(TEntity entity,
            CancellationToken ct = default)
        {
            return DbContextHelper<TDbContext>.UpdateAsync(entity, ct: ct);
        }

        public virtual void UpdateProperties(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            params string[] properties)
        {
            DbContextHelper<TDbContext>.UpdatePropertiesBatch(entities, properties, batchSize);
        }

        public virtual Task UpdatePropertiesAsync(IEnumerable<TEntity> entities, string[] properties,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            return DbContextHelper<TDbContext>.UpdatePropertiesBatchAsync(entities, properties, batchSize, ct);
        }

        public virtual void UpdateProperties(TEntity entity, params string[] properties)
        {
            DbContextHelper<TDbContext>.UpdateProperties(entity, properties);
        }

        public virtual Task UpdatePropertiesAsync(TEntity entity, string[] properties = null,
            CancellationToken ct = default)
        {
            return DbContextHelper<TDbContext>.UpdatePropertiesAsync(entity, properties, ct);
        }

        public virtual void UpdateDeletedProperties(TEntity entity)
        {
            var e = entity as IEntityDeleted;
            if (e == null)
            {
                return;
            }

            DbContextHelper<TDbContext>.UpdateProperties(entity,
                nameof(e.DeletedBy),
                nameof(e.DeletedAt));
        }

        public virtual Task UpdateDeletedPropertiesAsync(TEntity entity,
            CancellationToken ct = default)
        {
            var e = entity as IEntityDeleted;
            if (e == null)
            {
                return Task.CompletedTask;
            }

            return DbContextHelper<TDbContext>.UpdatePropertiesAsync(entity,
                new[]
                {
                    nameof(e.DeletedBy),
                    nameof(e.DeletedAt)
                },
                ct);
        }

        public virtual void UpdateDeletedProperties(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            if (!ReflectionHelper.ClassImplementsInterface<TEntity, IEntityDeleted>())
            {
                return;
            }

            DbContextHelper<TDbContext>.UpdatePropertiesBatch(entities,
                new[] {
                    nameof(IEntityDeleted.DeletedBy),
                    nameof(IEntityDeleted.DeletedAt)
                },
                batchSize);
        }

        public virtual Task UpdateDeletedPropertiesAsync(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            if (!ReflectionHelper.ClassImplementsInterface<TEntity, IEntityDeleted>())
            {
                return Task.CompletedTask;
            }

            return DbContextHelper<TDbContext>.UpdatePropertiesBatchAsync(entities,
                new[] {
                    nameof(IEntityDeleted.DeletedBy),
                    nameof(IEntityDeleted.DeletedAt)
                },
                batchSize,
                ct);
        }

        public virtual void Delete(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            DbContextHelper<TDbContext>.DeleteBatch(entities,
                batchSize: batchSize);
        }

        public virtual Task DeleteAsync(IEnumerable<TEntity> entities,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            return DbContextHelper<TDbContext>.DeleteBatchAsync(entities,
                batchSize: batchSize, ct: ct);
        }

        public virtual void Delete(TEntity entity)
        {
            DbContextHelper<TDbContext>.Delete(entity);
        }

        public virtual Task DeleteAsync(TEntity entity,
            CancellationToken ct = default)
        {
            return DbContextHelper<TDbContext>.DeleteAsync(entity, ct: ct);
        }

        public virtual void DeleteByIds(IEnumerable<TId> ids,
            byte batchSize = BATCH_SIZE_DEFAULT)
        {
            Validator.ValidateRequiredArgumentAndThrow(ids, nameof(ids));
            var entities = ids
                .Select(id => new TEntity { Id = id })
                .ToArray();
            DbContextHelper<TDbContext>.DeleteBatch(entities,
                batchSize: batchSize);
        }

        public virtual Task DeleteByIdsAsync(IEnumerable<TId> ids,
            byte batchSize = BATCH_SIZE_DEFAULT,
            CancellationToken ct = default)
        {
            Validator.ValidateRequiredArgumentAndThrow(ids, nameof(ids));
            var entities = ids
                .Select(id => new TEntity { Id = id })
                .ToArray();
            return DbContextHelper<TDbContext>.DeleteBatchAsync(entities,
                batchSize: batchSize, ct: ct);
        }

        public virtual void DeleteById(TId id)
        {
            DbContextHelper<TDbContext>.Delete(new TEntity { Id = id });
        }

        public virtual Task DeleteByIdAsync(TId id,
            CancellationToken ct = default)
        {
            return DbContextHelper<TDbContext>.DeleteAsync(new TEntity { Id = id }, ct: ct);
        }

        protected DbSet<TEntity> EntitySet(TDbContext ctx)
        {
            return ctx.Set<TEntity>();
        }

        protected TDbContext BuildContext()
        {
            return new TDbContext();
        }
    }
}
