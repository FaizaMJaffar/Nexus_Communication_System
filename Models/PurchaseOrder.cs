#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models;

// One line of the Admin's PURCHASE LIST: equipment ordered from a vendor.
// It can be linked to the customer order that needs the equipment. When the goods arrive
// ("Received") the quantity is added to the equipment stock automatically.
public class PurchaseOrder
{
    public int Id { get; set; }

    // PO-000001 ...
    [Display(Name = "Purchase No")]
    public string PurchaseNumber { get; set; } = "";

    [Required(ErrorMessage = "Select a vendor")]
    [Display(Name = "Vendor")]
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    [Required(ErrorMessage = "Equipment name is required")]
    [StringLength(100)]
    [Display(Name = "Equipment Name")]
    public string EquipmentName { get; set; } = "";

    [Required(ErrorMessage = "Equipment type is required")]
    [StringLength(100)]
    [Display(Name = "Equipment Type")]
    public string EquipmentType { get; set; } = "";

    [Range(1, 100000, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;

    [Range(0, 100000000, ErrorMessage = "Enter a valid price")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    // The customer order this purchase is for (optional)
    [Display(Name = "For Customer Order")]
    public int? CustomerOrderId { get; set; }
    public Order? CustomerOrder { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Order Date")]
    public DateTime OrderDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Expected Date")]
    public DateTime? ExpectedDate { get; set; }

    [Display(Name = "Received Date")]
    public DateTime? ReceivedDate { get; set; }

    // Ordered / Received / Cancelled
    [StringLength(20)]
    public string Status { get; set; } = "Ordered";

    [StringLength(300)]
    public string? Notes { get; set; }
}
