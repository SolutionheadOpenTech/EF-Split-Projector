using System.ComponentModel.DataAnnotations;

namespace Tests.TestContext.DataModels
{
    public class Packaging
    {
        [Key]
        public virtual int Id { get; set; }

        public virtual double Weight { get; set; }
    }
}