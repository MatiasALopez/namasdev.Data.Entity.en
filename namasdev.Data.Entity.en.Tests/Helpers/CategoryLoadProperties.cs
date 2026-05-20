using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace namasdev.Data.Entity.Tests.Helpers
{
    public class CategoryLoadProperties : ILoadProperties<TestEntity>
    {
        public IEnumerable<Expression<Func<TestEntity, object>>> BuildPaths()
        {
            yield return e => e.Category;
        }
    }
}
