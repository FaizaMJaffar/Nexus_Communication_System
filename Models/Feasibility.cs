#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models
{
    // Result of the Technical team's feasibility check for one order (specification rules):
    //   Dial-Up   : landline AND internet must both be available. If the customer already has
    //               a landline of this company, only the internet is checked.
    //   Broadband : internet availability at the address.
    //   Landline  : telephone line availability at the address.
    // For every type the server must also have free capacity.
    public class Feasibility
    {
        [Key]
        public int FeasibilityId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        // True when the customer already holds an Active landline of this company
        // (worked out by the system, not typed in by the technician).
        [Display(Name = "Existing Landline")]
        public bool HasExistingLandline { get; set; }

        // null = this check was not needed for the order
        [Display(Name = "Landline Available")]
        public bool? LandlineAvailable { get; set; }

        [Display(Name = "Internet Available")]
        public bool? InternetAvailable { get; set; }

        [Display(Name = "Server Available")]
        public bool ServerAvailable { get; set; } = true;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? Remarks { get; set; }

        [StringLength(100)]
        [Display(Name = "Checked By")]
        public string? CheckedBy { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Checked Date")]
        public DateTime CheckedDate { get; set; } = DateTime.Now;
    }

    // The rules in one place, so the controller and the views agree.
    public static class FeasibilityRules
    {
        public static bool NeedsLandlineCheck(string? connectionType, bool hasExistingLandline) =>
            connectionType == "Landline" || (connectionType == "Dial-Up" && !hasExistingLandline);

        public static bool NeedsInternetCheck(string? connectionType) =>
            connectionType == "Dial-Up" || connectionType == "Broadband";

        // Feasible only when every check that applies passed and the server has capacity.
        public static bool IsFeasible(string? connectionType, bool hasExistingLandline,
            bool? landline, bool? internet, bool server)
        {
            if (!server) return false;
            if (NeedsLandlineCheck(connectionType, hasExistingLandline) && landline != true) return false;
            if (NeedsInternetCheck(connectionType) && internet != true) return false;
            return true;
        }
    }
}
