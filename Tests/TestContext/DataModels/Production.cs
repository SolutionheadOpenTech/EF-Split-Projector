using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.TestContext.DataModels
{
    public class Production
    {
        [Key, Column(Order = 0, TypeName = "Date")]
        public virtual DateTime DataCreated { get; set; }
        [Key, Column(Order = 1)]
        public virtual int Sequence { get; set; }

        [Column(TypeName = "Date")]
        public virtual DateTime ScheduleDateCreated { get; set; }
        public virtual int ScheduleSequence { get; set; }

        public virtual int ProductionNumber { get; set; }

        [ForeignKey("ScheduleDateCreated, ScheduleSequence")]
        public virtual ProductionSchedule Schedule { get; set; }
        public virtual ICollection<Inventory> Results { get; set; }
    }
}