namespace NexusCommunicationSystem.Models;

// A connection order placed by a retail employee or online
public class Order
{
    public int Id { get; set; }
    // 11-character ORDER ID, e.g. D0000000001 (see NexusIds)
    public string OrderNumber { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; }

    public int? PlanId { get; set; }
    public Plan Plan { get; set; }

    // Snapshot of what the customer ordered
    public string ConnectionType { get; set; }
    public string PlanName { get; set; }
    public string Speed { get; set; }
    public decimal MonthlyCharges { get; set; }
    public decimal SecurityDeposit { get; set; }   // refundable, depends on the connection type
    // 12.24 % service tax on plan charges + deposit (see BillingRules)
    public decimal ServiceTax { get; set; }
    public decimal TotalAmount { get; set; }

    public string InstallAddress { get; set; }
    public string City { get; set; }

    public string PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";   // Unpaid / Paid
    public string Status { get; set; } = "Processing";      // Processing / Completed / Rejected

    // Set when the order was placed by a retail outlet employee (null = customer ordered online)
    public int? PlacedByEmployeeId { get; set; }
    public Employee PlacedByEmployee { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }

    // Filled in by the Technical team after a successful feasibility check
    public Connection Connection { get; set; }
    public ICollection<Feasibility> Feasibilities { get; set; }

    public string StatusCss => (Status ?? "").ToLower();
    public string PaymentCss => (PaymentStatus ?? "").ToLower();
}
