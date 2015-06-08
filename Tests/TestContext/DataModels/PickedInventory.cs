using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.TestContext.DataModels
{
    public class PickedInventory
    {
        [Key]
        [Column(Order = 0, TypeName = "Date")]
        public virtual DateTime DateCreated { get; set; }

        [Key]
        [Column(Order = 1)]
        public virtual int DateSequence { get; set; }

        public ICollection<PickedInventoryItem> Items { get; set; }
    }
}