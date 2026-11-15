using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models
{
    // Plans are managed by the Admin (Member 1) and chosen by customers (Member 2),
    // so this one table feeds both the Admin "Plans" screens and the Customer "Select Plan" step.
    public class Plan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Plan Name is required")]
        [StringLength(100)]
        [Display(Name = "Plan Name")]
        public string PlanName { get; set; }

        // Billing type: only Postpaid for now (specification)
        [Required(ErrorMessage = "Plan Type is required")]
        [StringLength(50)]
        [Display(Name = "Plan Type")]
        public string PlanType { get; set; }

        // Which kind of connection this plan belongs to (see PlanCatalog.Types).
        // The customer's "Select Plan" screen filters on this.
        [Required(ErrorMessage = "Connection Type is required")]
        [StringLength(50)]
        [Display(Name = "Connection Type")]
        public string ConnectionType { get; set; }

        // Free text shown to the customer, e.g. "50 Mbps", "500 Mins", "30 GB"
        [StringLength(50)]
        [Display(Name = "Speed / Allowance")]
        public string Speed { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Data (GB) is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Data (GB) must be 0 or more")]
        [Display(Name = "Data (GB)")]
        public int DataGB { get; set; }

        [Required(ErrorMessage = "Validity (Days) is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Validity (Days) must be 0 or more")]
        [Display(Name = "Validity (Days)")]
        public int ValidityDays { get; set; }

        // Plan features. Use "|" between items to show them as a bullet list to customers.
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
