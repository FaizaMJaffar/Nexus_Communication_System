using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Models;

namespace NexusCommunicationSystem.Data;

// Login helper shared by the Admin, Technical, Accounts and Retail portals.
// Every employee has his own user id + password (stored as a hash) and a Role.
public static class StaffAuth
{
    private static readonly PasswordHasher<Employee> Hasher = new();

    // One-way password hash
    public static string Hash(Employee employee, string password) =>
        Hasher.HashPassword(employee, password);

    // Returns the employee when user id + password are right, the employee is Active
    // and his Role matches the portal. Otherwise returns null.
    public static async Task<Employee> ValidateAsync(ApplicationDbContext db, string userId, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrEmpty(password)) return null;
        userId = userId.Trim();

        var employee = await db.Employees
            .Include(e => e.RetailShop)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Role == role && e.IsActive);

        if (employee == null || string.IsNullOrEmpty(employee.PasswordHash)) return null;

        var result = Hasher.VerifyHashedPassword(employee, employee.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : employee;
    }

    // Creates the login cookie. Name = full name of the employee, NameIdentifier = Employee.Id.
    public static async Task SignInAsync(HttpContext http, Employee employee, string role, bool persistent = false)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new Claim(ClaimTypes.Name, employee.FullName),
            new Claim(ClaimTypes.Role, role),
            new Claim("StaffUserId", employee.UserId ?? "")
        };
        if (employee.RetailShopId.HasValue)
        {
            claims.Add(new Claim("ShopId", employee.RetailShopId.Value.ToString()));
            claims.Add(new Claim("ShopName", employee.RetailShop?.ShopName ?? ""));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = persistent });
    }
}
