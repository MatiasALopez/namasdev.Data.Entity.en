using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;

using namasdev.Core.Entity;

namespace namasdev.Data.Entity
{
    public class DbContextBase : DbContext
    {
        public DbContextBase(
            string nameOrConnectionString,
            bool lazyLoadingEnabled = false,
            int? commandTimeout = null)
            : base(nameOrConnectionString)
        {
            Configuration.LazyLoadingEnabled = lazyLoadingEnabled;
            Database.CommandTimeout = commandTimeout;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Configurations.AddFromAssembly(GetType().Assembly);
        }

        public void Attach<T>(T entity, EntityState state,
            string[] propertiesToExcludeInUpdate = null)
            where T : class
        {
            Set<T>().Attach(entity);

            var entry = Entry(entity);
            entry.State = state;

            if (state == EntityState.Modified)
            {
                if (entity is IEntityCreated 
                    && propertiesToExcludeInUpdate == null)
                {
                    propertiesToExcludeInUpdate = new[]
                    {
                        nameof(IEntityCreated.CreatedBy),
                        nameof(IEntityCreated.CreatedAt),
                    };
                }

                SetPropertiesModifiedState(entry, propertiesToExcludeInUpdate,
                    isModified: false);
            }
        }

        public void AttachModifiedProperties<T>(T entity, string[] properties)
            where T : class
        {
            Set<T>().Attach(entity);
            SetPropertiesModifiedState(Entry(entity), properties);
        }

        public void SetPropertiesModifiedState<T>(DbEntityEntry<T> entry, string[] properties,
            bool isModified = true)
            where T : class
        {
            if (properties != null)
            {
                foreach (string p in properties)
                {
                    entry.Property(p).IsModified = isModified;
                }
            }
        }

        public TResult ExecuteQueryAndGet<TResult>(string query,
            params object[] parameters)
        {
            return Database
                .SqlQuery<TResult>(query, parameters)
                .FirstOrDefault();
        }

        public List<TResult> ExecuteQueryAndGetList<TResult>(string query,
            params object[] parameters)
        {
            return Database
                .SqlQuery<TResult>(query, parameters)
                .ToList();
        }

        public void ExecuteCommand(string command, 
            DbParameter[] parameters = null,
            TransactionalBehavior transactionalBehavior = TransactionalBehavior.DoNotEnsureTransaction)
        {
            Database.ExecuteSqlCommand(transactionalBehavior, command, parameters);
        }

        public TResult ExecuteCommandAndGet<TResult>(string command,
            Func<DbDataReader, TResult> recordMap,
            IEnumerable<DbParameter> parameters = null)
            where TResult : class
        {
            TResult result = null;

            ExecuteCommandUsingNewConnection(
                command,
                (cmd) => {
                    using (var reader = cmd.ExecuteReader())
                    {
                        result = recordMap(reader);
                    }
                },
                parameters: parameters);

            return result;
        }

        private void ExecuteCommandUsingNewConnection(string command, Action<DbCommand> action,
            IEnumerable<DbParameter> parameters = null)
        {
            using (var cmd = Database.Connection.CreateCommand())
            {
                cmd.CommandText = command;

                if (Database.CommandTimeout.HasValue)
                {
                    cmd.CommandTimeout = Database.CommandTimeout.Value;
                }

                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                }

                try
                {
                    Database.Connection.Open();

                    action(cmd);
                }
                finally
                {
                    Database.Connection.Close();
                }
            }
        }

        public ObjectResult<TEntity> MapReaderToEntity<TEntity>(DbDataReader reader)
        {
            return ((IObjectContextAdapter)this)
                .ObjectContext
                .Translate<TEntity>(reader);
        }
    }
}
