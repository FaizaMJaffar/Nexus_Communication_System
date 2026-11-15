using System.ComponentModel.DataAnnotations;

namespace NexusCommunicationSystem.Models;

// A registered customer
public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Order> Orders { get; set; }
    public ICollection<Connection> Connections { get; set; }
    public ICollection<Feedback> Feedbacks { get; set; }
}

// Fields of the customer registration form
public class RegisterViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; }

    [Required(ErrorMessage = "Contact number is required.")]
    [RegularExpression(@"^[0-9+\-\s]{10,15}$", ErrorMessage = "Please enter a valid contact number (e.g. 03001234567).")]
    [Display(Name = "Contact Number")]
    public string Phone { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(250)]
    public string Address { get; set; }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string City { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}

// Fields of the customer login form
public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string ReturnUrl { get; set; }
}

// Fields of the profile edit form
public class EditProfileViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; }

    public string Email { get; set; }

    [Required(ErrorMessage = "Contact number is required.")]
    [RegularExpression(@"^[0-9+\-\s]{10,15}$", ErrorMessage = "Please enter a valid contact number.")]
    [Display(Name = "Contact Number")]
    public string Phone { get; set; }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    public string City { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(250)]
    public string Address { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; }

    [StringLength(50, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmNewPassword { get; set; }
}

// Numbers shown on the customer dashboard
public class DashboardViewModel
{
    public Customer Customer { get; set; }
    public int TotalOrders { get; set; }
    public int ActiveConnections { get; set; }
    public int PendingConnections { get; set; }
    public decimal PendingBills { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
}
