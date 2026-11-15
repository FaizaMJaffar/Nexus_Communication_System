#nullable enable
namespace NexusCommunicationSystem.Models;

// View models for the Search & Reports module (Member 5).
// They read the shared Customer / Order / Connection / Bill / Payment / Feedback tables - no extra tables.

// A status name with how many records have it
public class StatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

// Payments of one method added up
public class MethodTotal
{
    public string Method { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Total { get; set; }
}

// Global search keyword and results
public class SearchViewModel
{
    public string? Keyword { get; set; }
    public bool Searched { get; set; }
    public List<Customer> Customers { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
    public int TotalResults => Customers.Count + Orders.Count + Connections.Count;
}

// Filters and results of the order search
public class OrderSearchViewModel
{
    public string? OrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool Searched { get; set; }
    public List<Order> Results { get; set; } = new();
}

// Filters and results of the connection search
public class ConnectionSearchViewModel
{
    public string? ConnectionNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? ConnectionType { get; set; }
    public string? Status { get; set; }
    public bool Searched { get; set; }
    public List<Connection> Results { get; set; } = new();
}

// Filters and results of the advanced search
public class AdvancedSearchViewModel
{
    public string? Keyword { get; set; }
    public string Category { get; set; } = "Orders";   // Customers / Orders / Connections / Bills
    public string? Contact { get; set; }          // customer's phone number
    public string? ConnectionType { get; set; }   // Dial-Up / Broadband / Landline
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public bool Searched { get; set; }

    public List<Customer> Customers { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
}

// One order looked up by its Order ID
public class OrderStatusViewModel
{
    public string? OrderNumber { get; set; }
    public bool Searched { get; set; }
    public Order? Order { get; set; }
    public decimal DueAmount { get; set; }
    public List<StatusCount> Summary { get; set; } = new();
}

// One connection looked up by its Account ID
public class ConnectionStatusViewModel
{
    public string? ConnectionNumber { get; set; }
    public bool Searched { get; set; }
    public Connection? Connection { get; set; }
    public decimal DueAmount { get; set; }
    public List<StatusCount> Summary { get; set; } = new();
}

// Date range shared by the reports
public class ReportFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

// One customer line in the customer report
public class CustomerReportRow
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderCount { get; set; }
    public int ConnectionCount { get; set; }
}

// Customer report
public class CustomerReportViewModel : ReportFilter
{
    public List<CustomerReportRow> Rows { get; set; } = new();
}

// Order report
public class OrderReportViewModel : ReportFilter
{
    public string? Status { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<StatusCount> StatusSummary { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

// Billing report
public class BillingReportViewModel : ReportFilter
{
    public string? PaidStatus { get; set; }   // Paid / Partial / Unpaid / blank = all
    public List<Bill> Bills { get; set; } = new();
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalUnpaid { get; set; }
}

// Payment report
public class PaymentReportViewModel : ReportFilter
{
    public string? Method { get; set; }
    public List<Payment> Payments { get; set; } = new();
    public List<MethodTotal> ByMethod { get; set; } = new();
    public decimal TotalCollected { get; set; }
}

// Feedback report
public class FeedbackReportViewModel : ReportFilter
{
    public int? Rating { get; set; }
    public List<Feedback> Feedbacks { get; set; } = new();
    public double AverageRating { get; set; }
    public Dictionary<int, int> Distribution { get; set; } = new();
}
