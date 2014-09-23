using System.Data.Entity;
using Tests.TestContext.DataModels;

namespace Tests.TestContext
{
    public class TestContext : DbContext
    {
        public DbSet<Inventory> Inventory { get; set; }
    }
}
