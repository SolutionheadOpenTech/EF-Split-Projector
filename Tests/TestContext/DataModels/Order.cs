using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.TestContext.DataModels
{
    public class Order
    {
        [Key]
        [Column(Order = 0, TypeName = "Date")]
        public virtual DateTime DateCreated { get; set; }

        [Key]
        [Column(Order = 1)]
        public virtual int DateSequence { get; set; }

        [ForeignKey("DateCreated, DateSequence")]
        public virtual PickedInventory PickedInventory { get; set; }
    }
}