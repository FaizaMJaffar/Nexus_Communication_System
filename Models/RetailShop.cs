using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models
{
    // A retail outlet where orders are placed
    public class RetailShop
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Shop Name is required")]
        [StringLength(100)]
        [Display(Name = "Shop Name")]
        public string ShopName { get; set; }

        [Required(ErrorMessage = "Owner Name is required")]
        [StringLength(100)]
        [Display(Name = "Owner Name")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; }

        [StringLength(250)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [StringLength(20)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

    }
}
