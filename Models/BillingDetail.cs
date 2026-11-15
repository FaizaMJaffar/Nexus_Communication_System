#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models
{
    // One line on a bill (plan, equipment...)
    public class BillingDetail
    {
        [Key]
        public int BillingDetailId { get; set; }

        public int BillId { get; set; }

        [Required]
        public string? Description { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
         public Bill? Bill { get; set;  }



    }
}
