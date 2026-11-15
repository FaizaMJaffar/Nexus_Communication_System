#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace NexusCommunicationSystem.Models
{
    // A monthly bill of one connection, made by Accounts
    public class Bill
    {
        [Key]
        public int BillId { get; set; }

        public int CustomerId {  get; set; }
        public int ConnectionId { get; set; }
        public DateTime BillDate { get; set; }
        public DateTime DueDate { get; set; }
        [Required]
        public string? BillingMonth {  get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }

        // 12.24 % service tax on the whole bill (see BillingRules)
        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceTax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Carry-forward, saved when the bill is made: what is still unpaid on the earlier bills of this
        // connection, and what was already paid on them. TotalAmount stays the charges of THIS bill only,
        // so the earlier bills keep their own status and nothing is counted twice.
        [Column(TypeName = "decimal(18,2)")]
        public decimal PreviousBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PreviousPayments { get; set; }

        [NotMapped]
        public decimal TotalPayable => TotalAmount + PreviousBalance;

        public string Status { get; set; } = "Unpaid";

         public int? DiscountId { get; set; }

        public Customer? Customer { get; set; }

        public Connection? Connection { get; set; }

 
public ICollection<BillingDetail> BillingDetails { get; set; } = new List<BillingDetail>();

public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    }
}
