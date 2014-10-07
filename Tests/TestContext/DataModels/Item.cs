using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tests.TestContext.DataModels
{
    public class Item
    {
        [Key]
        [Column(Order = 0)]
        public virtual int Id { get; set; }

        public virtual string Description { get; set; }
    }
}