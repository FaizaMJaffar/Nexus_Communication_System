using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCommunicationSystem.Models
{
    // The four kinds of staff (specification: every employee gets his own user id and password).
    public static class StaffRoles
    {
        public const string Admin = "Admin";
        public const string Technical = "Technical";
        public const string Accounts = "Accounts";
        public const string Retail = "Retail";

        public static readonly string[] All = { Admin, Technical, Accounts, Retail };
    }

    // Staff member with his own login and role
    public class Employee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        [StringLength(100)]
        [Display(Name = "Designation")]
        public string Designation { get; set; }

        [Required(ErrorMessage = "Salary is required")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Salary")]
        public decimal Salary { get; set; }

        [Required(ErrorMessage = "Join Date is required")]
        [Display(Name = "Join Date")]
        public DateTime JoinDate { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // ---- Login credentials ----
        // Role decides which portal the employee can log in to (Admin / Technical / Accounts / Retail).
        [Required(ErrorMessage = "Role is required")]
        [StringLength(20)]
        [Display(Name = "Role (Portal)")]
        public string Role { get; set; } = StaffRoles.Technical;

        [Required(ErrorMessage = "User ID is required")]
        [StringLength(50)]
        [RegularExpression(@"^[A-Za-z0-9._-]{3,50}$", ErrorMessage = "User ID must be 3-50 characters: letters, digits, dot, dash or underscore.")]
        [Display(Name = "User ID")]
        public string UserId { get; set; }

        // Only the hash is stored in the database
        public string PasswordHash { get; set; }

        // Typed in the Admin form; never saved as plain text
        [NotMapped]
        [DataType(DataType.Password)]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        // Retail employees belong to a retail shop (outlet); other roles leave this empty
        [Display(Name = "Retail Shop")]
        public int? RetailShopId { get; set; }
        public RetailShop RetailShop { get; set; }
    }
}
