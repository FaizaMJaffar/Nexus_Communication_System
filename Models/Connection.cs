using System.ComponentModel.DataAnnotations;

namespace NexusCommunicationSystem.Models;

// One connection per approved order.
// Created by the Technical team (Member 3) once the area is feasible; the customer (Member 2)
// sees it under "My Connections" and Accounts (Member 4) bills against it.
public class Connection
{
    public int Id { get; set; }

    [Display(Name = "Account ID")]
    // The 16-character ACCOUNT ID, e.g. B101000000000001 (see NexusIds)
    public string ConnectionNumber { get; set; }

    public int? OrderId { get; set; }
    public Order Order { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; }

    public int? PlanId { get; set; }
    public Plan Plan { get; set; }

    // Snapshot of the plan at the time of ordering (stays correct even if the Admin edits the plan later)
    public string ConnectionType { get; set; }
    public string PlanName { get; set; }
    public string Speed { get; set; }
    public decimal MonthlyCharges { get; set; }

    public string InstallAddress { get; set; }
    public string City { get; set; }

    // Pending / Active / Temporarily Inactive / Permanently Inactive
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Activated Date")]
    public DateTime? ActivatedAt { get; set; }

    [Display(Name = "Deactivated Date")]
    public DateTime? DeactivatedAt { get; set; }

    [StringLength(500)]
    public string Remarks { get; set; }

    // "Temporarily Inactive" -> "temporarily-inactive" (usable as a css class)
    public string StatusCss => (Status ?? "").ToLower().Replace(' ', '-');
}
