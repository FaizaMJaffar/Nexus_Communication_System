using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models
{
    // One stock line. Admin (Member 1) manages stock/prices/vendors,
    // Technical (Member 3) adds items and assigns them to connections.
    public class Equipment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Equipment Name is required")]
        [StringLength(100)]
        [Display(Name = "Equipment Name")]
        public string EquipmentName { get; set; }

        [Required(ErrorMessage = "Equipment Type is required")]
        [StringLength(100)]
        [Display(Name = "Equipment Type")]
        public string EquipmentType { get; set; }

        [StringLength(100)]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or more")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Unit Price is required")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        // Available / In Use / Under Repair / Damaged
        [Required(ErrorMessage = "Status is required")]
        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Available";

        [StringLength(250)]
        [Display(Name = "Location")]
        public string Location { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [Display(Name = "Vendor")]
        public int? VendorId { get; set; }

        [ForeignKey(nameof(VendorId))]
        public Vendor Vendor { get; set; }
    }
}
