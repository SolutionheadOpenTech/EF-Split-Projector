using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.TestContext.DataModels
{
    public class Warehouse
    {
        [Key, Column(Order = 0)]
        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual ICollection<WarehouseLocation> Locations { get; set; }
    }
}