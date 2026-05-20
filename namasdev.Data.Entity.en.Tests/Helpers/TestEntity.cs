using System;
using System.ComponentModel.DataAnnotations.Schema;

using namasdev.Core.Entity;

namespace namasdev.Data.Entity.Tests.Helpers
{
    public class TestEntity : IEntity<int>, IEntityCreated, IEntityDeleted
    {
        // IEntity<int> — explicit Ids so seed values are deterministic (no identity)
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        // IEntityCreated
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // IEntityDeleted
        public string DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool Deleted { get; set; }

        // One property per primitive type
        public int IntValue { get; set; }
        public short ShortValue { get; set; }
        public long LongValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public string StringValue { get; set; }
        public bool BoolValue { get; set; }

        // Navigation property (for loadProperties / Include tests)
        public int? CategoryId { get; set; }
        public TestCategory Category { get; set; }
    }
}
