#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace NexusCommunicationSystem.Models
{
    // A payment made against a bill
    public class Payment
    {

        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int BillId { get; set; }

        public DateTime PaymentDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        public Bill? Bill { get; set; }

        public string? ReferenceNo{ get; set; }

        [Required]
        public string? PaymentMethod { get; set; }
    }
}
