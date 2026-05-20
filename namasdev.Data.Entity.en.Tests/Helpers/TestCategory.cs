using System.ComponentModel.DataAnnotations.Schema;

using namasdev.Core.Entity;

namespace namasdev.Data.Entity.Tests.Helpers
{
    public class TestCategory : IEntity<int>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
