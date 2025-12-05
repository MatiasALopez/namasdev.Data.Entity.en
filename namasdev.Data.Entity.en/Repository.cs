using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;

using namasdev.Core.Entity;
using namasdev.Core.Validation;

namespace namasdev.Data.Entity
{
    public class Repository<TDbContext, TEntity, TId> : ReadOnlyRepository<TDbContext, TEntity, TId>, IRepository<TEntity, TId>
        where TDbContext : DbContextBase, new()
        where TEntity : class, IEntity<TId>, new()
        where TId : IEquatable<TId>
    {
        private const int BATCH_SIZE_DEFAULT = 100;

        public virtual void Add(IEnumerable<TEntity> entities,
            int batchSize = BATCH_SIZE_DEFAULT)
        {
            DbContextHelper<TDbContext>.AddBatch(entities,
                batchSize: batchSize);
        }

        public virtual void Add(TEntity entity)
        {
            DbContextHelper<TDbContext>.Add(entity);
        }

        public virtual void Update(IEnumerable<TEntity> entities,
            int batchSize = BATCH_SIZE_DEFAULT)
        {
            DbContextHelper<TDbContext>.UpdateBatch(entities, 
                batchSize: batchSize);
        }

        public virtual void Update(TEntity entity)
        {
            DbContextHelper<TDbContext>.Update(entity);
        }

        public virtual void UpdateProperties(IEnumerable<TEntity> entities, 
            int batchSize = BATCH_SIZE_DEFAULT, 
            params string[] properties)
        {
            DbContextHelper<TDbContext>.UpdatePropertiesBatch(entities, properties, batchSize);
        }

        public virtual void UpdateProperties(TEntity entity, params string[] properties)
        {
            DbContextHelper<TDbContext>.UpdateProperties(entity, properties);
        }

        public virtual void UpdateDeletedFields(TEntity entity)
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

        public virtual void UpdateDeletedFields(IEnumerable<TEntity> entities, 
            int batchSize = BATCH_SIZE_DEFAULT)
        {
            if (typeof(TEntity) is IEntityDeleted)
            {
                DbContextHelper<TDbContext>.UpdatePropertiesBatch(entities, 
                    new[] {
                        nameof(IEntityDeleted.DeletedBy),
                        nameof(IEntityDeleted.DeletedAt)
                    }, 
                    batchSize);
            }
        }

        public virtual void Delete(IEnumerable<TEntity> entities,
            int batchSize = BATCH_SIZE_DEFAULT)
        {
            DbContextHelper<TDbContext>.DeleteBatch(entities, 
                batchSize: batchSize);
        }

        public virtual void Delete(TEntity entity)
        {
            DbContextHelper<TDbContext>.Delete(entity);
        }

        public virtual void DeleteByIds(IEnumerable<TId> ids, 
            int batchSize = BATCH_SIZE_DEFAULT)
        {
            Validator.ValidateRequiredArgumentAndThrow(ids, nameof(ids));
            var entities = ids
                .Select(id => new TEntity { Id = id })
                .ToArray();
            DbContextHelper<TDbContext>.DeleteBatch(entities, 
                batchSize: batchSize);
        }

        public virtual void DeleteById(TId id)
        {
            DbContextHelper<TDbContext>.Delete(new TEntity { Id = id });
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
