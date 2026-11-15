#nullable enable
using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Data;

namespace NexusCommunicationSystem.Models;

// "Track status" lookup used by the Customer and Retail portals (specification):
// the person types an ORDER ID or an ACCOUNT ID and sees the status, the amount still due
// and the customer's contact number. The caller passes the orders it is allowed to see,
// so a customer only finds his own orders and a retail employee only his shop's orders.

// One order found by the status lookup
public class StatusResult
{
    public string OrderNumber { get; set; } = "";
    public string? AccountId { get; set; }
    public string CustomerName { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string ConnectionType { get; set; } = "";
    public string PlanName { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public string OrderStatus { get; set; } = "";
    public string FeasibilityStatus { get; set; } = "Pending";
    public string? ConnectionStatus { get; set; }
    public string PaymentStatus { get; set; } = "";
    public decimal DueAmount { get; set; }
}

// Query and results of the status lookup page
public class StatusLookupViewModel
{
    public string? Query { get; set; }
    public bool Searched { get; set; }
    public List<StatusResult> Results { get; set; } = new();
}

// Finds orders by Order ID or Account ID
public static class StatusLookup
{
    public static async Task<StatusLookupViewModel> FindAsync(ApplicationDbContext db, IQueryable<Order> allowedOrders, string? query)
    {
        var vm = new StatusLookupViewModel { Query = query };
        if (string.IsNullOrWhiteSpace(query)) return vm;

        var id = query.Trim();
        vm.Searched = true;

        var orders = await allowedOrders
            .Include(o => o.Customer)
            .Include(o => o.Connection)
            .Include(o => o.Feasibilities)
            .Where(o => o.OrderNumber.Contains(id)
                     || (o.Connection != null && o.Connection.ConnectionNumber.Contains(id)))
            .OrderByDescending(o => o.OrderDate)
            .Take(20)
            .ToListAsync();

        // Unpaid bills of the connections found (what the customer still owes)
        var connectionIds = orders.Where(o => o.Connection != null).Select(o => o.Connection!.Id).ToList();
        var openBills = await db.Bills
            .Include(b => b.Payments)
            .Where(b => connectionIds.Contains(b.ConnectionId) && b.Status != "Paid")
            .ToListAsync();

        foreach (var o in orders)
        {
            // bills still unpaid, minus what was already paid against them
            var billsDue = o.Connection == null ? 0m : openBills
                .Where(b => b.ConnectionId == o.Connection.Id)
                .Sum(b => Math.Max(0m, b.TotalAmount - b.Payments.Sum(p => p.AmountPaid)));
            // the first invoice of the order itself, if it is not paid yet
            var orderDue = o.PaymentStatus == "Unpaid" && o.Status != "Rejected" ? o.TotalAmount : 0m;

            vm.Results.Add(new StatusResult
            {
                OrderNumber = o.OrderNumber,
                AccountId = o.Connection?.ConnectionNumber,
                CustomerName = o.Customer?.FullName ?? "-",
                ContactNumber = o.Customer?.Phone ?? "-",
                ConnectionType = o.ConnectionType,
                PlanName = o.PlanName,
                OrderDate = o.OrderDate,
                OrderStatus = o.Status,
                FeasibilityStatus = o.Feasibilities?.OrderByDescending(f => f.FeasibilityId).FirstOrDefault()?.Status ?? "Pending",
                ConnectionStatus = o.Connection?.Status,
                PaymentStatus = o.PaymentStatus,
                DueAmount = billsDue + orderDue
            });
        }
        return vm;
    }
}
