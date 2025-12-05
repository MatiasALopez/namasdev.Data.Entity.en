using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

using namasdev.Core.Entity;

namespace namasdev.Data.Entity.Config
{
    public abstract class EntityDeletedConfigBase<TEntity> : EntityTypeConfiguration<TEntity>
        where TEntity : class, IEntityDeleted
    {
        public EntityDeletedConfigBase()
        {
            Property(e => e.Deleted)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
        }
    }
}
