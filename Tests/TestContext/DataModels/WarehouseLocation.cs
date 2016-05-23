using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.TestContext.DataModels
{
    public class WarehouseLocation
    {
        [Key, Column(Order = 0)]
        public virtual int WarehouseId { get; set; }
        [Key, Column(Order = 1)]
        public virtual int LocationId { get; set; }

        public virtual string Description { get; set; }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }
    }
}