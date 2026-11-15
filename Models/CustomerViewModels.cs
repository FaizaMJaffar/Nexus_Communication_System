using System.ComponentModel.DataAnnotations;

namespace NexusCommunicationSystem.Models;

// Static lookup lists used by the customer screens.
// The actual plans (name, price, speed ...) now live in the Plans table, managed by the Admin.
public static class PlanCatalog
{
    // The three connection types of the specification.
    // Dial-Up needs a landline of the same vendor; "Landline" = telephone-only connection.
    public static readonly List<string> Types = new()
    {
        "Dial-Up", "Broadband", "Landline"
    };

    // Refundable security deposit charged per connection type (specification, Financial I)
    public static readonly Dictionary<string, decimal> SecurityDeposits = new()
    {
        { "Dial-Up", 325m },
        { "Broadband", 500m },
        { "Landline", 250m }
    };

    public static decimal DepositFor(string connectionType) =>
        connectionType != null && SecurityDeposits.TryGetValue(connectionType, out var d) ? d : 0m;

    public static readonly Dictionary<string, string> Icons = new()
    {
        { "Dial-Up", "📞" },
        { "Broadband", "🌐" },
        { "Landline", "☎️" }
    };

    public static readonly Dictionary<string, string> Descriptions = new()
    {
        { "Dial-Up", "Internet over your landline (a landline of the same vendor is required)" },
        { "Broadband", "Always-on broadband internet for your home or office" },
        { "Landline", "Telephone-only connection with local and STD calling" }
    };

    // 3-digit numeric code of every city of the territory (used inside the 16-character account id)
    public static readonly Dictionary<string, string> CityCodes = new()
    {
        { "Karachi", "101" }, { "Lahore", "102" }, { "Islamabad", "103" }, { "Rawalpindi", "104" },
        { "Faisalabad", "105" }, { "Multan", "106" }, { "Hyderabad", "107" }, { "Peshawar", "108" },
        { "Quetta", "109" }, { "Sialkot", "110" }, { "Gujranwala", "111" }
    };

    public static readonly List<string> PaymentMethods = new()
    {
        "Cash on Installation", "JazzCash", "EasyPaisa", "Bank Transfer", "Debit / Credit Card"
    };

    public static readonly List<string> Cities = new()
    {
        "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Faisalabad", "Multan",
        "Hyderabad", "Peshawar", "Quetta", "Sialkot", "Gujranwala"
    };
}

// Step 1 of an order: type and address
public class ApplyConnectionViewModel
{
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
}

// Step 2 of an order: the chosen plan and payment method
public class SelectPlanViewModel
{
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
}
