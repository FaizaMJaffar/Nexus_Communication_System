using System.ComponentModel.DataAnnotations;

namespace NexusCommunicationSystem.Models;

// Retail portal: a retail employee places orders for customers who walk in to the outlet.

// Step 1 of a new order: connection type + installation address
public class RetailOrderStartViewModel
{
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Please select a connection type.")]
    [Display(Name = "Connection Type")]
    public string ConnectionType { get; set; } = PlanCatalog.Types[0];

    [Required(ErrorMessage = "Installation address is required.")]
    [StringLength(250)]
    [Display(Name = "Installation Address")]
    public string InstallAddress { get; set; }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string City { get; set; }

    // for display only
    public Customer Customer { get; set; }
}

// Step 2 of a new order: plan + payment method
public class RetailSelectPlanViewModel
{
    public int CustomerId { get; set; }

    [Required]
    public string ConnectionType { get; set; }

    [Required]
    [StringLength(250)]
    public string InstallAddress { get; set; }

    [Required]
    [StringLength(100)]
    public string City { get; set; }

    [Required(ErrorMessage = "Please select a plan.")]
    public int? PlanId { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = PlanCatalog.PaymentMethods[0];

    public List<Plan> Plans { get; set; } = new();

    // for display only
    public Customer Customer { get; set; }
}

// Retail portal: a retail employee records a payment received at the outlet for a bill
public class RetailPaymentViewModel
{
    public static readonly string[] Methods = { "Cash", "Card", "Bank Transfer" };

    [Required(ErrorMessage = "Please select a bill.")]
    [Display(Name = "Bill")]
    public int? BillId { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Enter an amount greater than zero.")]
    [Display(Name = "Amount Paid")]
    public decimal AmountPaid { get; set; }

    [Required(ErrorMessage = "Please select a payment method.")]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "Cash";

    [StringLength(100)]
    [Display(Name = "Reference No")]
    public string ReferenceNo { get; set; }
}
