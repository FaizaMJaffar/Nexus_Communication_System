#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models
{
    // Links a piece of equipment to a connection
    public class EquipmentAssignment
    {

        [Key]
        public int EquipmentAssignmentId { get; set; }

        [Required]
        public int ConnectionId { get; set; }

        [ForeignKey("ConnectionId")]
        public Connection? Connection { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [ForeignKey(nameof(EquipmentId))]
        public Equipment? Equipment { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Equipment Type")]
        public string EquipmentType { get; set; } = string.Empty;

        [StringLength(17)]
        [RegularExpression(@"^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$",
            ErrorMessage = "Incorrect MAC Address (i.e. jaise E0:67:B3:91:AA:40)")]
        public string? MacAddress { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Assigned Date")]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

    }
}
