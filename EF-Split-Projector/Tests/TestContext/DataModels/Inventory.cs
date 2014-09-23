using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.TestContext.DataModels
{
    public class Inventory
    {
        [Key]
        [Column(Order = 0, TypeName = "Date")]
        public virtual DateTime DateCreated { get; set; }

        public virtual int DateSequence { get; set; }
    }
}
