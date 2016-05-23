using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.TestContext.DataModels
{
    public class ProductionSchedule
    {
        [Key, Column(Order =  0, TypeName = "Date")]
        public virtual DateTime DataCreated { get; set; }
        [Key, Column(Order = 1)]
        public virtual int Sequence { get; set; }

        public virtual DateTime Start { get; set; }
        public virtual DateTime End { get; set; }

        public virtual ICollection<Production> Productions { get; set; }
    }
}