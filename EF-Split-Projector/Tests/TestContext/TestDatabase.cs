using System.Data.Entity;
using Tests.TestContext.DataModels;

namespace Tests.TestContext
{
    public class TestDatabase : DbContext
    {
        public TestDatabase()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<TestDatabase>());
        }

        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Packaging> Packaging { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseLocation> WarehouseLocations { get; set; }
    }
}
