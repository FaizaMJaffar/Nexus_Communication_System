using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Models;

namespace NexusCommunicationSystem.Data;

// ONE DbContext for the whole project (Members 1-4 + later Member 5 search/reports).
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // ---- Member 1 (Admin) ----
    public DbSet<Employee> Employees { get; set; }
    public DbSet<RetailShop> RetailShops { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<Equipment> Equipments { get; set; }

    // ---- Member 2 (Customer) ----
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Connection> Connections { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }

    // ---- Member 3 (Technical) ----
    public DbSet<Feasibility> Feasibilities { get; set; }
    public DbSet<EquipmentAssignment> EquipmentAssignments { get; set; }

    // ---- Member 4 (Accounts) ----
    public DbSet<Bill> Bills { get; set; }
    public DbSet<BillingDetail> BillingDetails { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Discount> Discounts { get; set; }

    // Table relations and delete rules
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ================= Member 1 =================
        modelBuilder.Entity<Equipment>()
            .HasOne(e => e.Vendor)
            .WithMany()
            .HasForeignKey(e => e.VendorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Purchase list: deleting a customer order never deletes the purchase line, and a vendor with purchase lines is kept
        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            // the vendor is mandatory on a purchase line, so a vendor that has purchase lines cannot be deleted (see Admin DeleteVendor)
            e.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CustomerOrder).WithMany().HasForeignKey(x => x.CustomerOrderId).OnDelete(DeleteBehavior.SetNull);
        });

        // Employees: own login (unique user id) + optional retail shop.
        // Restrict: a shop that still has employees cannot be deleted (Admin screen shows a message).
        modelBuilder.Entity<Employee>(e =>
        {
            e.Property(x => x.Role).IsRequired().HasMaxLength(20);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.UserId).IsUnique();

            e.HasOne(x => x.RetailShop).WithMany()
                .HasForeignKey(x => x.RetailShopId).OnDelete(DeleteBehavior.Restrict);
        });

        // ================= Member 2 =================
        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(c => c.FullName).IsRequired().HasMaxLength(100);
            e.Property(c => c.Email).IsRequired().HasMaxLength(256);
            e.Property(c => c.Phone).IsRequired().HasMaxLength(20);
            e.Property(c => c.Address).IsRequired().HasMaxLength(250);
            e.Property(c => c.City).IsRequired().HasMaxLength(100);
            e.Property(c => c.PasswordHash).IsRequired();
            e.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.OrderNumber).IsRequired().HasMaxLength(30);
            e.Property(o => o.ConnectionType).IsRequired().HasMaxLength(50);
            e.Property(o => o.PlanName).IsRequired().HasMaxLength(100);
            e.Property(o => o.Speed).HasMaxLength(50);
            e.Property(o => o.InstallAddress).IsRequired().HasMaxLength(250);
            e.Property(o => o.City).IsRequired().HasMaxLength(100);
            e.Property(o => o.PaymentMethod).IsRequired().HasMaxLength(50);
            e.Property(o => o.PaymentStatus).IsRequired().HasMaxLength(20);
            e.Property(o => o.Status).IsRequired().HasMaxLength(20);
            e.Property(o => o.MonthlyCharges).HasPrecision(18, 2);
            e.Property(o => o.SecurityDeposit).HasPrecision(18, 2);
            e.Property(o => o.ServiceTax).HasPrecision(18, 2);
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.TotalAmount).HasPrecision(18, 2);

            // Restrict avoids SQL Server "multiple cascade paths" errors
            e.HasOne(o => o.Customer).WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);

            // Orders placed by a retail employee keep a link to him (an employee with orders cannot be deleted)
            e.HasOne(o => o.PlacedByEmployee).WithMany()
                .HasForeignKey(o => o.PlacedByEmployeeId).OnDelete(DeleteBehavior.Restrict);

            // If the Admin deletes a plan, old orders keep their plan snapshot (PlanName / charges)
            e.HasOne(o => o.Plan).WithMany()
                .HasForeignKey(o => o.PlanId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Connection>(e =>
        {
            e.Property(c => c.ConnectionNumber).IsRequired().HasMaxLength(30);
            e.Property(c => c.ConnectionType).IsRequired().HasMaxLength(50);
            e.Property(c => c.PlanName).IsRequired().HasMaxLength(100);
            e.Property(c => c.Speed).HasMaxLength(50);
            e.Property(c => c.InstallAddress).IsRequired().HasMaxLength(250);
            e.Property(c => c.City).IsRequired().HasMaxLength(100);
            e.Property(c => c.Status).IsRequired().HasMaxLength(20);
            e.Property(c => c.Remarks).HasMaxLength(500);
            e.Property(c => c.MonthlyCharges).HasPrecision(18, 2);
            e.HasIndex(c => c.ConnectionNumber).IsUnique();

            e.HasOne(c => c.Customer).WithMany(cu => cu.Connections)
                .HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Restrict);

            // one order -> at most one connection (created by the Technical team)
            e.HasOne(c => c.Order).WithOne(o => o.Connection)
                .HasForeignKey<Connection>(c => c.OrderId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Plan).WithMany()
                .HasForeignKey(c => c.PlanId).OnDelete(DeleteBehavior.SetNull);
        });

        // ================= Member 3 =================
        modelBuilder.Entity<Feasibility>()
            .HasOne(f => f.Order).WithMany(o => o.Feasibilities)
            .HasForeignKey(f => f.OrderId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EquipmentAssignment>(e =>
        {
            e.HasOne(a => a.Connection).WithMany()
                .HasForeignKey(a => a.ConnectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Equipment).WithMany()
                .HasForeignKey(a => a.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // ================= Member 4 =================
        modelBuilder.Entity<Bill>(e =>
        {
            e.HasOne(b => b.Customer).WithMany()
                .HasForeignKey(b => b.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Connection).WithMany()
                .HasForeignKey(b => b.ConnectionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}