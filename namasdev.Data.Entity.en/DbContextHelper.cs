using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using namasdev.Core.Entity;
using namasdev.Core.Validation;

namespace namasdev.Data.Entity
{
    public class DbContextHelper<TDbContext>
        where TDbContext : DbContextBase, new()
    {
        public static void Config()
        {
            Database.SetInitializer<TDbContext>(null);
        }

        public static void Add<T>(T entity)
            where T : class
        {
            AttachAndSaveChanges(entity, EntityState.Added);
        }

        public static void AddBatch<T>(IEnumerable<T> entities,
            int batchSize = 100)
            where T : class
        {
            AttachBatch(entities, EntityState.Added, 
                batchSize: batchSize);
        }

        public static void Update<T>(T entity,
            bool excludeCreatedProperties = true,
            bool excludeUpdatedProperties = true)
            where T : class
        {
            AttachAndSaveChanges(
                entity, 
                EntityState.Modified,
                propertiesToExcludeInUpdate: BuildPropertiesToExcludeInUpdate<T>(
                    excludeCreatedProperties: excludeCreatedProperties,
                    excludeUpdatedProperties: excludeUpdatedProperties));
        }

        public static void UpdateBatch<T>(IEnumerable<T> entities,
            bool excludeCreatedProperties = true, 
            bool excludeUpdatedProperties = true,
            int batchSize = 100)
            where T : class
        {
            AttachBatch(
                entities, 
                EntityState.Modified,
                propertiesToExcludeInUpdate: BuildPropertiesToExcludeInUpdate<T>(
                    excludeCreatedProperties: excludeCreatedProperties,
                    excludeUpdatedProperties: excludeUpdatedProperties),
                batchSize: batchSize);
        }

        public static void Delete<T>(T entity)
            where T : class
        {
            AttachAndSaveChanges(entity, EntityState.Deleted);
        }

        public static void DeleteBatch<T>(IEnumerable<T> entities,
            int batchSize = 100)
            where T : class
        {
            AttachBatch(entities, EntityState.Deleted, 
                batchSize: batchSize);
        }

        private static void AttachAndSaveChanges<T>(T entity, EntityState state,
            string[] propertiesToExcludeInUpdate = null)
            where T : class
        {
            using (var ctx = new TDbContext())
            {
                ctx.Attach(entity, state, 
                    propertiesToExcludeInUpdate: propertiesToExcludeInUpdate);

                ctx.SaveChanges();
            }
        }

        public static void UpdateProperties<T>(T entity, params string[] properties)
           where T : class
        {
            Validator.ValidateRequiredListArgumentAndThrow(properties, nameof(properties), validateNotEmpty: false);

            if (!properties.Any())
            {
                return;
            }

            using (var ctx = new TDbContext())
            {
                ctx.AttachModifiedProperties(entity, properties);

                ctx.Configuration.ValidateOnSaveEnabled = false;
                ctx.SaveChanges();
            }
        }

        public static void UpdatePropertiesBatch<T>(IEnumerable<T> entities, string[] properties,
            int batchSize = 100)
            where T : class
        {
            Validator.ValidateRequiredListArgumentAndThrow(properties, nameof(properties), validateNotEmpty: false);

            if (!properties.Any())
            {
                return;
            }

            ActionBatch(entities,
                (ctx, entity) => ctx.AttachModifiedProperties(entity, properties),
                batchSize: batchSize,
                dbContextConstructor: () =>
                {
                    var ctx = new TDbContext();
                    ctx.Configuration.ValidateOnSaveEnabled = false;
                    return ctx;
                });
        }

        private static void AttachBatch<T>(IEnumerable<T> entities, EntityState state,
            string[] propertiesToExcludeInUpdate = null,
            int batchSize = 100)
            where T : class
        {
            ActionBatch(entities,
                (ctx, entity) => ctx.Attach(entity, state, propertiesToExcludeInUpdate),
                batchSize: batchSize);
        }

        private static void ActionBatch<T>(IEnumerable<T> entities, Action<TDbContext, T> action,
            int batchSize = 100,
            Func<TDbContext> dbContextConstructor = null) 
            where T : class
        {
            Validator.ValidateRequiredListArgumentAndThrow(entities, nameof(entities), validateNotEmpty: false);

            if (!entities.Any())
            {
                return;
            }

            batchSize = Math.Min(100, batchSize);

            dbContextConstructor = dbContextConstructor ?? (() => new TDbContext());

            var ctx = dbContextConstructor();
            try
            {
                int count = 0;
                foreach (var entity in entities)
                {
                    action(ctx, entity);
                    count++;

                    if (count == batchSize)
                    {
                        ctx.SaveChanges();
                        ctx.Dispose();

                        ctx = dbContextConstructor();

                        count = 0;
                    }
                }

                if (count > 0)
                {
                    ctx.SaveChanges();
                }
            }
            finally
            {
                if (ctx != null)
                {
                    ctx.Dispose();
                }
            }
        }

        private static string[] BuildPropertiesToExcludeInUpdate<T>(bool excludeCreatedProperties, bool excludeUpdatedProperties)
        {
            var properties = new List<string>();

            if (excludeCreatedProperties
                && typeof(IEntityCreated).IsAssignableFrom(typeof(T)))
            {
                properties.AddRange(new[]
                {
                    nameof(IEntityCreated.CreatedBy),
                    nameof(IEntityCreated.CreatedAt)
                });
            }
            if (excludeUpdatedProperties
                && typeof(IEntityDeleted).IsAssignableFrom(typeof(T)))
            {
                properties.AddRange(new[]
                {
                    nameof(IEntityDeleted.DeletedBy),
                    nameof(IEntityDeleted.DeletedAt)
                });
            }

            return properties.Any()
                ? properties.ToArray()
                : null;
        }
    }
}
