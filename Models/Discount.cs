#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models
{
    // A discount (the bulk slabs are seeded)
    public class Discount
    {
        [Key]
        public int DiscountId { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000)]
        public decimal Value { get; set; }

        public bool IsActive { get; set;  }
    }
}
