#nullable enable
using System.ComponentModel.DataAnnotations;

namespace NexusCommunicationSystem.Models;

// One row of the Technical "Work Orders Queue":
// a real customer order together with the result of its latest feasibility check (if any).
public class WorkOrderRow
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string ConnectionType { get; set; } = "";
    public string PlanName { get; set; } = "";
    public string City { get; set; } = "";

    // "Pending" when no feasibility check has been saved yet
    public string FeasibilityStatus { get; set; } = "Pending";
    public int? FeasibilityId { get; set; }
    public string? CheckedBy { get; set; }

    public int? ConnectionId { get; set; }
}

// An order with its latest feasibility check
public class WorkOrderDetails
{
    public Order Order { get; set; } = null!;
    public Feasibility? Feasibility { get; set; }
}

// Form data for creating a connection
public class CreateConnectionViewModel
{
    [Required(ErrorMessage = "Please select a work order.")]
    [Display(Name = "Work Order")]
    public int? OrderId { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }
}
