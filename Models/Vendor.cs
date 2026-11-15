using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models
{
    // A supplier we buy equipment from
    public class Vendor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vendor Name is required")]
        [StringLength(100)]
        [Display(Name = "Vendor Name")]
        public string VendorName { get; set; }

        [Required(ErrorMessage = "Contact Person is required")]
        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [StringLength(20)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [StringLength(250)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

    }
}
