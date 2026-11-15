#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Data;
using NexusCommunicationSystem.Models;

namespace NexusCommunicationSystem.Controllers
{
    // Search & Reports module (Member 5). Read-only. It shows every customer's data, so it is Admin only.
    [Authorize(Roles = "Admin")]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GLOBAL SEARCH
        [HttpGet]
        public async Task<IActionResult> Index(string? keyword)
        {
            var model = new SearchViewModel { Keyword = keyword };

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                model.Searched = true;

                model.Customers = await _context.Customers
                    .Where(c => c.FullName.Contains(keyword)
                             || c.Email.Contains(keyword)
                             || c.Phone.Contains(keyword)
                             || c.City.Contains(keyword))
                    .OrderBy(c => c.FullName).Take(50).ToListAsync();

                model.Orders = await _context.Orders.Include(o => o.Customer)
                    .Where(o => o.OrderNumber.Contains(keyword)
                             || o.ConnectionType.Contains(keyword)
                             || o.PlanName.Contains(keyword)
                             || o.Status.Contains(keyword)
                             || o.Customer.FullName.Contains(keyword))
                    .OrderByDescending(o => o.OrderDate).Take(50).ToListAsync();

                model.Connections = await _context.Connections.Include(c => c.Customer)
                    .Where(c => c.ConnectionNumber.Contains(keyword)
                             || c.ConnectionType.Contains(keyword)
                             || c.PlanName.Contains(keyword)
                             || c.Status.Contains(keyword)
                             || c.Customer.FullName.Contains(keyword))
                    .OrderByDescending(c => c.CreatedAt).Take(50).ToListAsync();
            }

            return View(model);
        }

        // 2. SEARCH ORDER
        [HttpGet]
        public async Task<IActionResult> SearchOrder(OrderSearchViewModel model)
        {
            bool hasFilter = !string.IsNullOrWhiteSpace(model.OrderNumber)
                || !string.IsNullOrWhiteSpace(model.CustomerName)
                || !string.IsNullOrWhiteSpace(model.Status)
                || model.FromDate.HasValue || model.ToDate.HasValue;

            if (hasFilter)
            {
                model.Searched = true;
                var query = _context.Orders.Include(o => o.Customer).AsQueryable();

                if (!string.IsNullOrWhiteSpace(model.OrderNumber))
                    query = query.Where(o => o.OrderNumber.Contains(model.OrderNumber.Trim()));
                if (!string.IsNullOrWhiteSpace(model.CustomerName))
                    query = query.Where(o => o.Customer.FullName.Contains(model.CustomerName.Trim()));
                if (!string.IsNullOrWhiteSpace(model.Status))
                    query = query.Where(o => o.Status == model.Status);
                if (model.FromDate.HasValue)
                    query = query.Where(o => o.OrderDate >= model.FromDate.Value.Date);
                if (model.ToDate.HasValue)
                    query = query.Where(o => o.OrderDate < model.ToDate.Value.Date.AddDays(1));

                model.Results = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            }

            return View(model);
        }

        // 3. SEARCH CONNECTION
        [HttpGet]
        public async Task<IActionResult> SearchConnection(ConnectionSearchViewModel model)
        {
            bool hasFilter = !string.IsNullOrWhiteSpace(model.ConnectionNumber)
                || !string.IsNullOrWhiteSpace(model.CustomerName)
                || !string.IsNullOrWhiteSpace(model.ConnectionType)
                || !string.IsNullOrWhiteSpace(model.Status);

            if (hasFilter)
            {
                model.Searched = true;
                var query = _context.Connections.Include(c => c.Customer).AsQueryable();

                if (!string.IsNullOrWhiteSpace(model.ConnectionNumber))
                    query = query.Where(c => c.ConnectionNumber.Contains(model.ConnectionNumber.Trim()));
                if (!string.IsNullOrWhiteSpace(model.CustomerName))
                    query = query.Where(c => c.Customer.FullName.Contains(model.CustomerName.Trim()));
                if (!string.IsNullOrWhiteSpace(model.ConnectionType))
                    query = query.Where(c => c.ConnectionType == model.ConnectionType);
                if (!string.IsNullOrWhiteSpace(model.Status))
                    query = query.Where(c => c.Status == model.Status);

                model.Results = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
            }

            return View(model);
        }

        // 4. ADVANCED SEARCH
        [HttpGet]
        public async Task<IActionResult> AdvancedSearch(AdvancedSearchViewModel model)
        {
            // first visit (no query string): just show the empty form
            if (!Request.Query.Any())
                return View(model);

            model.Searched = true;
            var kw = model.Keyword?.Trim();
            var from = model.FromDate?.Date;
            var to = model.ToDate?.Date.AddDays(1);
            var contact = model.Contact?.Trim();
            var cType = model.ConnectionType?.Trim();

            switch (model.Category)
            {
                case "Customers":
                    var cq = _context.Customers.AsQueryable();
                    if (!string.IsNullOrEmpty(kw))
                        cq = cq.Where(c => c.FullName.Contains(kw) || c.Email.Contains(kw) || c.Phone.Contains(kw) || c.City.Contains(kw));
                    if (!string.IsNullOrEmpty(contact)) cq = cq.Where(c => c.Phone.Contains(contact));
                    if (from.HasValue) cq = cq.Where(c => c.CreatedAt >= from.Value);
                    if (to.HasValue) cq = cq.Where(c => c.CreatedAt < to.Value);
                    model.Customers = await cq.OrderBy(c => c.FullName).ToListAsync();
                    break;

                case "Connections":
                    var nq = _context.Connections.Include(c => c.Customer).AsQueryable();
                    if (!string.IsNullOrEmpty(kw))
                        nq = nq.Where(c => c.ConnectionNumber.Contains(kw) || c.ConnectionType.Contains(kw) || c.PlanName.Contains(kw) || c.Customer.FullName.Contains(kw));
                    if (!string.IsNullOrEmpty(contact)) nq = nq.Where(c => c.Customer.Phone.Contains(contact));
                    if (!string.IsNullOrEmpty(cType)) nq = nq.Where(c => c.ConnectionType == cType);
                    if (!string.IsNullOrEmpty(model.Status)) nq = nq.Where(c => c.Status == model.Status);
                    if (from.HasValue) nq = nq.Where(c => c.CreatedAt >= from.Value);
                    if (to.HasValue) nq = nq.Where(c => c.CreatedAt < to.Value);
                    model.Connections = await nq.OrderByDescending(c => c.CreatedAt).ToListAsync();
                    break;

                case "Bills":
                    var bq = _context.Bills.Include(b => b.Customer).AsQueryable();
                    if (!string.IsNullOrEmpty(kw))
                        bq = bq.Where(b => b.Customer!.FullName.Contains(kw) || b.BillingMonth!.Contains(kw));
                    if (!string.IsNullOrEmpty(contact)) bq = bq.Where(b => b.Customer!.Phone.Contains(contact));
                    if (!string.IsNullOrEmpty(model.Status)) bq = bq.Where(b => b.Status == model.Status);
                    if (from.HasValue) bq = bq.Where(b => b.BillDate >= from.Value);
                    if (to.HasValue) bq = bq.Where(b => b.BillDate < to.Value);
                    if (model.MinAmount.HasValue) bq = bq.Where(b => b.TotalAmount >= model.MinAmount.Value);
                    if (model.MaxAmount.HasValue) bq = bq.Where(b => b.TotalAmount <= model.MaxAmount.Value);
                    model.Bills = await bq.OrderByDescending(b => b.BillDate).ToListAsync();
                    break;

                default: // Orders
                    model.Category = "Orders";
                    var oq = _context.Orders.Include(o => o.Customer).AsQueryable();
                    if (!string.IsNullOrEmpty(kw))
                        oq = oq.Where(o => o.OrderNumber.Contains(kw) || o.ConnectionType.Contains(kw) || o.PlanName.Contains(kw) || o.Customer.FullName.Contains(kw));
                    if (!string.IsNullOrEmpty(contact)) oq = oq.Where(o => o.Customer.Phone.Contains(contact));
                    if (!string.IsNullOrEmpty(cType)) oq = oq.Where(o => o.ConnectionType == cType);
                    if (!string.IsNullOrEmpty(model.Status)) oq = oq.Where(o => o.Status == model.Status);
                    if (from.HasValue) oq = oq.Where(o => o.OrderDate >= from.Value);
                    if (to.HasValue) oq = oq.Where(o => o.OrderDate < to.Value);
                    if (model.MinAmount.HasValue) oq = oq.Where(o => o.TotalAmount >= model.MinAmount.Value);
                    if (model.MaxAmount.HasValue) oq = oq.Where(o => o.TotalAmount <= model.MaxAmount.Value);
                    model.Orders = await oq.OrderByDescending(o => o.OrderDate).ToListAsync();
                    break;
            }

            return View(model);
        }

        // 5. ORDER STATUS
        [HttpGet]
        public async Task<IActionResult> OrderStatus(string? orderNumber)
        {
            var model = new OrderStatusViewModel { OrderNumber = orderNumber };

            if (!string.IsNullOrWhiteSpace(orderNumber))
            {
                model.Searched = true;
                var no = orderNumber.Trim();
                model.Order = await _context.Orders.Include(o => o.Customer).Include(o => o.Connection)
                    .FirstOrDefaultAsync(o => o.OrderNumber == no);
                if (model.Order != null)
                {
                    model.DueAmount = await DueForConnection(model.Order.Connection?.Id);
                    if (model.Order.PaymentStatus == "Unpaid" && model.Order.Status != "Rejected")
                        model.DueAmount += model.Order.TotalAmount;
                }
            }

            model.Summary = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return View(model);
        }

        // 6. CONNECTION STATUS
        [HttpGet]
        public async Task<IActionResult> ConnectionStatus(string? connectionNumber)
        {
            var model = new ConnectionStatusViewModel { ConnectionNumber = connectionNumber };

            if (!string.IsNullOrWhiteSpace(connectionNumber))
            {
                model.Searched = true;
                var no = connectionNumber.Trim();
                model.Connection = await _context.Connections.Include(c => c.Customer)
                    .FirstOrDefaultAsync(c => c.ConnectionNumber == no);
                if (model.Connection != null)
                    model.DueAmount = await DueForConnection(model.Connection.Id);
            }

            model.Summary = await _context.Connections
                .GroupBy(c => c.Status)
                .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return View(model);
        }

        // 7. CUSTOMER REPORT
        [HttpGet]
        public async Task<IActionResult> CustomerReport(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Customers.AsQueryable();
            if (fromDate.HasValue) query = query.Where(c => c.CreatedAt >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(c => c.CreatedAt < toDate.Value.Date.AddDays(1));

            var model = new CustomerReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Rows = await query.OrderBy(c => c.FullName)
                    .Select(c => new CustomerReportRow
                    {
                        CustomerId = c.Id,
                        FullName = c.FullName,
                        Email = c.Email,
                        Phone = c.Phone,
                        City = c.City,
                        CreatedAt = c.CreatedAt,
                        OrderCount = c.Orders.Count,
                        ConnectionCount = c.Connections.Count
                    }).ToListAsync()
            };

            return View(model);
        }

        // 8. ORDER REPORT
        [HttpGet]
        public async Task<IActionResult> OrderReport(DateTime? fromDate, DateTime? toDate, string? status)
        {
            var query = _context.Orders.Include(o => o.Customer).AsQueryable();
            if (fromDate.HasValue) query = query.Where(o => o.OrderDate >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(o => o.OrderDate < toDate.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            var model = new OrderReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Status = status,
                Orders = orders,
                TotalAmount = orders.Sum(o => o.TotalAmount),
                StatusSummary = orders.GroupBy(o => o.Status)
                    .Select(g => new StatusCount { Status = g.Key, Count = g.Count() }).ToList()
            };

            return View(model);
        }

        // 9. BILLING REPORT
        [HttpGet]
        public async Task<IActionResult> BillingReport(DateTime? fromDate, DateTime? toDate, string? paidStatus)
        {
            var query = _context.Bills.Include(b => b.Customer).Include(b => b.Payments).AsQueryable();
            if (fromDate.HasValue) query = query.Where(b => b.BillDate >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(b => b.BillDate < toDate.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(paidStatus)) query = query.Where(b => b.Status == paidStatus);

            var bills = await query.OrderByDescending(b => b.BillDate).ToListAsync();
            decimal paid = bills.Sum(b => b.Payments.Sum(p => p.AmountPaid));

            var model = new BillingReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                PaidStatus = paidStatus,
                Bills = bills,
                TotalBilled = bills.Sum(b => b.TotalAmount),
                TotalPaid = paid,
                TotalUnpaid = Math.Max(0, bills.Sum(b => b.TotalAmount) - paid)
            };

            return View(model);
        }

        // 10. PAYMENT REPORT
        [HttpGet]
        public async Task<IActionResult> PaymentReport(DateTime? fromDate, DateTime? toDate, string? method)
        {
            var query = _context.Payments
                .Include(p => p.Bill).ThenInclude(b => b!.Customer)
                .AsQueryable();
            if (fromDate.HasValue) query = query.Where(p => p.PaymentDate >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(p => p.PaymentDate < toDate.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(method)) query = query.Where(p => p.PaymentMethod == method);

            var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();

            var model = new PaymentReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Method = method,
                Payments = payments,
                TotalCollected = payments.Sum(p => p.AmountPaid),
                ByMethod = payments.GroupBy(p => p.PaymentMethod ?? "-")
                    .Select(g => new MethodTotal { Method = g.Key, Count = g.Count(), Total = g.Sum(x => x.AmountPaid) })
                    .ToList()
            };

            return View(model);
        }

        // 11. FEEDBACK REPORT
        [HttpGet]
        public async Task<IActionResult> FeedbackReport(DateTime? fromDate, DateTime? toDate, int? rating)
        {
            var query = _context.Feedbacks.Include(f => f.Customer).AsQueryable();
            if (fromDate.HasValue) query = query.Where(f => f.CreatedAt >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(f => f.CreatedAt < toDate.Value.Date.AddDays(1));
            if (rating.HasValue) query = query.Where(f => f.Rating == rating.Value);

            var list = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();

            var model = new FeedbackReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Rating = rating,
                Feedbacks = list,
                AverageRating = list.Count > 0 ? Math.Round(list.Average(f => f.Rating), 2) : 0,
                Distribution = Enumerable.Range(1, 5).ToDictionary(r => r, r => list.Count(f => f.Rating == r))
            };

            return View(model);
        }

        // Unpaid bills of a connection, minus what was already paid on them
        private async Task<decimal> DueForConnection(int? connectionId)
        {
            if (!connectionId.HasValue) return 0m;
            var bills = await _context.Bills.Include(b => b.Payments)
                .Where(b => b.ConnectionId == connectionId.Value && b.Status != "Paid")
                .ToListAsync();
            return bills.Sum(b => Math.Max(0m, b.TotalAmount - b.Payments.Sum(p => p.AmountPaid)));
        }
    }
}
