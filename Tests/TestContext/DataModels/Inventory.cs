using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.TestContext.DataModels
{
    public class Inventory
    {
        [Key, Column(Order = 0, TypeName = "Date")]
        public virtual DateTime DateCreated { get; set; }
        [Key, Column(Order = 1)]
        public virtual int DateSequence { get; set; }

        public virtual int ItemId { get; set; }
        public virtual int WarehouseId { get; set; }
        public virtual int LocationId { get; set; }
        public virtual int Quantity { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
        [ForeignKey("WarehouseId, LocationId")]
        public virtual WarehouseLocation Location { get; set; }
    }
}