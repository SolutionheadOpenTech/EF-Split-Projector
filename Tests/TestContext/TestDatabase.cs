﻿using System.Data.Entity;
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
        public DbSet<Order> Orders { get; set; }
        public DbSet<PickedInventory> PickedInventory { get; set; }
        public DbSet<PickedInventoryItem> PickedInventoryItem { get; set; }
        public DbSet<ProductionSchedule> ProductionSchedules { get; set; }
        public DbSet<Production> Productions { get; set; }
    }
}
