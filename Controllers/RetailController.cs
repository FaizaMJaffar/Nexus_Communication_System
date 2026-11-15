using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Data;
using NexusCommunicationSystem.Models;

namespace NexusCommunicationSystem.Controllers;

// Retail portal. Orders are placed ONLY by retail outlet employees (specification); they also track the
// orders they placed: feasibility status, connections (account ids), bills and payments.
// Every list is limited to the orders of the employee's own retail shop.
[Authorize(Roles = "Retail")]
public class RetailController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<Customer> _hasher = new();

    public RetailController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ----- who is logged in (claims are written by StaffAuth.SignInAsync) -----
    private int EmployeeId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    private int? ShopId
    {
        get
        {
            var v = User.FindFirstValue("ShopId");
            return int.TryParse(v, out var id) ? id : (int?)null;
        }
    }

    // Orders of this employee's retail shop (or only his own orders when he has no shop)
    private IQueryable<Order> MyOrders()
    {
        var shopId = ShopId;
        var employeeId = EmployeeId;
        return shopId.HasValue
            ? _db.Orders.Where(o => o.PlacedByEmployee.RetailShopId == shopId.Value)
            : _db.Orders.Where(o => o.PlacedByEmployeeId == employeeId);
    }

    // Connections that belong to those orders
    private async Task<List<int>> MyConnectionIds()
    {
        var orderIds = await MyOrders().Select(o => o.Id).ToListAsync();
        return await _db.Connections
            .Where(c => c.OrderId.HasValue && orderIds.Contains(c.OrderId.Value))
            .Select(c => c.Id)
            .ToListAsync();
    }

    // Put the staff and shop name on every page
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["StaffName"] = User.Identity?.Name ?? "Retail Employee";
        ViewData["ShopName"] = User.FindFirstValue("ShopName") ?? "";
        base.OnActionExecuting(context);
    }

    // ===================== LOGIN / LOGOUT =====================
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.IsInRole("Retail")) return RedirectToAction(nameof(Dashboard));
        return View();
    }

    // Retail staff login
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, bool remember)
    {
        var employee = await StaffAuth.ValidateAsync(_db, username, password, StaffRoles.Retail);
        if (employee != null)
        {
            await StaffAuth.SignInAsync(HttpContext, employee, StaffRoles.Retail, remember);
            return RedirectToAction(nameof(Dashboard));
        }

        ViewBag.Error = "Wrong user ID or password, or this account is not active.";
        ViewBag.Username = username;
        return View();
    }

    // Sign out the retail employee
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ===================== DASHBOARD =====================
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalOrders = await MyOrders().CountAsync();
        ViewBag.InProgress = await MyOrders().CountAsync(o => o.Status == "Processing");
        ViewBag.Completed = await MyOrders().CountAsync(o => o.Status == "Completed");
        ViewBag.Rejected = await MyOrders().CountAsync(o => o.Status == "Rejected");

        var orderIds = await MyOrders().Select(o => o.Id).ToListAsync();
        ViewBag.ActiveConnections = await _db.Connections
            .CountAsync(c => c.OrderId.HasValue && orderIds.Contains(c.OrderId.Value) && c.Status == "Active");

        var recent = await MyOrders()
            .Include(o => o.Customer)
            .Include(o => o.Feasibilities)
            .OrderByDescending(o => o.OrderDate)
            .Take(8)
            .ToListAsync();
        return View(recent);
    }

    // ===================== TRACK STATUS =====================
    // Status by Order ID or Account ID, limited to the orders of this employee's outlet
    [HttpGet]
    public async Task<IActionResult> TrackStatus(string q)
    {
        return View(await StatusLookup.FindAsync(_db, MyOrders(), q));
    }

    // ===================== CUSTOMERS =====================
    public async Task<IActionResult> Customers(string q)
    {
        var query = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(c => c.FullName.Contains(q) || c.Email.Contains(q) || c.Phone.Contains(q) || c.City.Contains(q));
        }
        ViewBag.Q = q;
        return View(await query.OrderByDescending(c => c.Id).Take(100).ToListAsync());
    }

    // Show the customer registration form
    [HttpGet]
    public IActionResult RegisterCustomer()
    {
        return View(new RegisterViewModel());
    }

    // Register a customer at the counter
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterCustomer(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim().ToLower();
        if (await _db.Customers.AnyAsync(c => c.Email == email))
        {
            ModelState.AddModelError("Email", "A customer with this email already exists.");
            return View(model);
        }

        var customer = new Customer
        {
            FullName = model.FullName.Trim(),
            Email = email,
            Phone = model.Phone.Trim(),
            City = model.City,
            Address = model.Address.Trim(),
            CreatedAt = DateTime.Now
        };
        customer.PasswordHash = _hasher.HashPassword(customer, model.Password);

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Customer registered. You can place the order now.";
        return RedirectToAction(nameof(NewOrder), new { customerId = customer.Id });
    }

    // ===================== NEW ORDER (step 1: type + address) =====================
    [HttpGet]
    public async Task<IActionResult> NewOrder(int customerId, string type = null)
    {
        var customer = await _db.Customers.FindAsync(customerId);
        if (customer == null)
        {
            TempData["Error"] = "Customer not found. Choose a customer first.";
            return RedirectToAction(nameof(Customers));
        }

        var vm = new RetailOrderStartViewModel
        {
            CustomerId = customer.Id,
            InstallAddress = customer.Address,
            City = customer.City,
            Customer = customer
        };
        if (!string.IsNullOrEmpty(type) && PlanCatalog.Types.Contains(type))
            vm.ConnectionType = type;
        return View(vm);
    }

    // Step 1 of a new order: connection type and address
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewOrder(RetailOrderStartViewModel model)
    {
        var customer = await _db.Customers.FindAsync(model.CustomerId);
        if (customer == null)
        {
            TempData["Error"] = "Customer not found. Choose a customer first.";
            return RedirectToAction(nameof(Customers));
        }
        model.Customer = customer;

        if (!PlanCatalog.Types.Contains(model.ConnectionType ?? ""))
            ModelState.AddModelError("ConnectionType", "Please select a valid connection type.");

        if (!ModelState.IsValid) return View(model);

        return RedirectToAction(nameof(SelectPlan), new
        {
            customerId = model.CustomerId,
            type = model.ConnectionType,
            address = model.InstallAddress,
            city = model.City
        });
    }

    // ===================== NEW ORDER (step 2: plan + payment) =====================
    [HttpGet]
    public async Task<IActionResult> SelectPlan(int customerId, string type, string address, string city)
    {
        var customer = await _db.Customers.FindAsync(customerId);
        if (customer == null) return RedirectToAction(nameof(Customers));

        if (string.IsNullOrEmpty(type) || !PlanCatalog.Types.Contains(type) ||
            string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(city))
        {
            return RedirectToAction(nameof(NewOrder), new { customerId });
        }

        return View(new RetailSelectPlanViewModel
        {
            CustomerId = customerId,
            ConnectionType = type,
            InstallAddress = address,
            City = city,
            Plans = await LoadPlans(type),
            Customer = customer
        });
    }

    // Step 2: choose the plan and place the order
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPlan(RetailSelectPlanViewModel model)
    {
        var customer = await _db.Customers.FindAsync(model.CustomerId);
        if (customer == null) return RedirectToAction(nameof(Customers));
        model.Customer = customer;
        model.Plans = await LoadPlans(model.ConnectionType ?? "");

        var plan = model.PlanId.HasValue
            ? model.Plans.FirstOrDefault(p => p.Id == model.PlanId.Value)
            : null;

        if (model.PlanId.HasValue && plan == null)
            ModelState.AddModelError("PlanId", "Invalid plan selected.");

        if (!PlanCatalog.PaymentMethods.Contains(model.PaymentMethod ?? ""))
            ModelState.AddModelError("PaymentMethod", "Please select a valid payment method.");

        if (!ModelState.IsValid || plan == null) return View(model);

        var now = DateTime.Now;

        // Order id: type letter + 10-digit serial (D / B / T), e.g. D0000000001
        var typeCode = NexusIds.TypeCode(plan.ConnectionType);
        var existingNumbers = await _db.Orders
            .Where(o => o.OrderNumber.StartsWith(typeCode))
            .Select(o => o.OrderNumber)
            .ToListAsync();
        var deposit = PlanCatalog.DepositFor(plan.ConnectionType);

        // Same order as the customer portal creates, plus the retail employee who placed it.
        // The Technical team does the feasibility check and creates the connection afterwards.
        // service tax is charged on the whole first invoice (plan + deposit)
        var tax = BillingRules.Round((plan.Price + deposit) * BillingRules.ServiceTaxPercent / 100m);
        var order = new Order
        {
            OrderNumber = NexusIds.NextOrderNumber(existingNumbers, typeCode),
            CustomerId = customer.Id,
            PlanId = plan.Id,
            ConnectionType = plan.ConnectionType,
            PlanName = plan.PlanName,
            Speed = plan.Speed,
            MonthlyCharges = plan.Price,
            SecurityDeposit = deposit,
            ServiceTax = tax,
            TotalAmount = plan.Price + deposit + tax,
            InstallAddress = model.InstallAddress,
            City = model.City,
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = "Unpaid",
            Status = "Processing",
            OrderDate = now,
            DueDate = now.AddDays(7),
            PlacedByEmployeeId = EmployeeId
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Order {order.OrderNumber} placed for {customer.FullName}.";
        return RedirectToAction(nameof(OrderDetails), new { id = order.Id });
    }

    // Fill the plan list for the chosen connection type
    private Task<List<Plan>> LoadPlans(string type) =>
        _db.Plans.Where(p => p.IsActive && p.ConnectionType == type)
            .OrderBy(p => p.Price)
            .ToListAsync();

    // ===================== ORDERS (track by order id / account id) =====================
    public async Task<IActionResult> Orders(string q, string status)
    {
        var query = MyOrders()
            .Include(o => o.Customer)
            .Include(o => o.Connection)
            .Include(o => o.Feasibilities)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(o => o.OrderNumber.Contains(q)
                                  || o.Customer.FullName.Contains(q)
                                  || (o.Connection != null && o.Connection.ConnectionNumber.Contains(q)));
        }
        if (!string.IsNullOrEmpty(status) && status != "All")
            query = query.Where(o => o.Status == status);

        ViewBag.Q = q;
        ViewBag.Status = string.IsNullOrEmpty(status) ? "All" : status;
        return View(await query.OrderByDescending(o => o.OrderDate).Take(200).ToListAsync());
    }

    // Order details with feasibility, account id and bills
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await MyOrders()
            .Include(o => o.Customer)
            .Include(o => o.PlacedByEmployee)
            .Include(o => o.Connection)
            .Include(o => o.Feasibilities)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction(nameof(Orders));
        }

        // bills of the connection (created by Accounts once the connection exists)
        ViewBag.Bills = order.Connection == null
            ? new List<Bill>()
            : await _db.Bills.Include(b => b.Payments)
                .Where(b => b.ConnectionId == order.Connection.Id)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();

        return View(order);
    }

    // ===================== CONNECTIONS =====================
    public async Task<IActionResult> Connections(string q)
    {
        var orderIds = await MyOrders().Select(o => o.Id).ToListAsync();
        var query = _db.Connections
            .Include(c => c.Customer)
            .Include(c => c.Order)
            .Where(c => c.OrderId.HasValue && orderIds.Contains(c.OrderId.Value));

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(c => c.ConnectionNumber.Contains(q) || c.Customer.FullName.Contains(q)
                                  || c.PlanName.Contains(q) || c.Status.Contains(q));
        }
        ViewBag.Q = q;
        return View(await query.OrderByDescending(c => c.CreatedAt).Take(200).ToListAsync());
    }

    // ===================== BILLS =====================
    public async Task<IActionResult> Bills(string q)
    {
        var connectionIds = await MyConnectionIds();
        var query = _db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .Include(b => b.Payments)
            .Where(b => connectionIds.Contains(b.ConnectionId));

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(b => b.Customer.FullName.Contains(q) || b.Connection.ConnectionNumber.Contains(q)
                                  || b.Status.Contains(q));
        }
        ViewBag.Q = q;
        return View(await query.OrderByDescending(b => b.BillDate).Take(200).ToListAsync());
    }

    // ===================== PAYMENTS =====================
    public async Task<IActionResult> Payments(string q)
    {
        var connectionIds = await MyConnectionIds();
        var query = _db.Payments
            .Include(p => p.Bill).ThenInclude(b => b.Customer)
            .Include(p => p.Bill).ThenInclude(b => b.Connection)
            .Where(p => connectionIds.Contains(p.Bill.ConnectionId));

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p => p.Bill.Customer.FullName.Contains(q) || p.Bill.Connection.ConnectionNumber.Contains(q)
                                  || p.ReferenceNo.Contains(q));
        }
        ViewBag.Q = q;
        return View(await query.OrderByDescending(p => p.PaymentDate).Take(200).ToListAsync());
    }

    // ===================== RECEIVE PAYMENTS (specification: retail outlets maintain payments) =====================

    // The customer paid the first invoice of the order (plan + security deposit + tax) at the outlet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceiveOrderPayment(int id)
    {
        var order = await MyOrders().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null || order.PaymentStatus == "Paid" || order.Status == "Rejected")
        {
            TempData["Error"] = "This order invoice cannot be paid.";
            return RedirectToAction(nameof(Orders));
        }

        order.PaymentStatus = "Paid";
        order.PaidDate = DateTime.Now;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Payment of ${order.TotalAmount:N2} received for order {order.OrderNumber}.";
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    // Show the form to record a payment against a bill of this outlet
    [HttpGet]
    public async Task<IActionResult> AddPayment(int? billId)
    {
        await LoadPayableBills();
        return View(new RetailPaymentViewModel { BillId = billId });
    }

    // Record the payment and update the bill status (Partial / Paid)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayment(RetailPaymentViewModel model)
    {
        if (!RetailPaymentViewModel.Methods.Contains(model.PaymentMethod ?? ""))
            ModelState.AddModelError(nameof(model.PaymentMethod), "Please select a valid payment method.");

        Bill bill = null;
        decimal alreadyPaid = 0m;
        if (model.BillId.HasValue)
        {
            var connectionIds = await MyConnectionIds();
            bill = await _db.Bills.Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BillId == model.BillId.Value && connectionIds.Contains(b.ConnectionId));

            if (bill == null)
            {
                ModelState.AddModelError(nameof(model.BillId), "Select a bill of your outlet.");
            }
            else
            {
                alreadyPaid = bill.Payments.Sum(p => p.AmountPaid);
                var balance = bill.TotalAmount - alreadyPaid;
                if (balance <= 0)
                    ModelState.AddModelError(nameof(model.BillId), "This bill is already fully paid.");
                else if (model.AmountPaid > balance)
                    ModelState.AddModelError(nameof(model.AmountPaid), $"The amount is more than the balance of ${balance:N2}.");
            }
        }

        if (!ModelState.IsValid || bill == null)
        {
            await LoadPayableBills();
            return View(model);
        }

        _db.Payments.Add(new Payment
        {
            BillId = bill.BillId,
            PaymentDate = DateTime.Now,
            AmountPaid = model.AmountPaid,
            PaymentMethod = model.PaymentMethod,
            ReferenceNo = string.IsNullOrWhiteSpace(model.ReferenceNo) ? null : model.ReferenceNo.Trim()
        });
        bill.Status = alreadyPaid + model.AmountPaid >= bill.TotalAmount ? "Paid" : "Partial";
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Payment of ${model.AmountPaid:N2} recorded for bill #{bill.BillId}.";
        return RedirectToAction(nameof(Payments));
    }

    // Bills of this outlet that still have a balance
    private async Task LoadPayableBills()
    {
        var connectionIds = await MyConnectionIds();
        var bills = await _db.Bills
            .Include(b => b.Customer)
            .Include(b => b.Connection)
            .Include(b => b.Payments)
            .Where(b => connectionIds.Contains(b.ConnectionId) && b.Status != "Paid")
            .OrderByDescending(b => b.BillDate)
            .ToListAsync();

        ViewBag.Bills = bills
            .Select(b => new { b, due = b.TotalAmount - b.Payments.Sum(p => p.AmountPaid) })
            .Where(x => x.due > 0)
            .Select(x => new SelectListItem
            {
                Value = x.b.BillId.ToString(),
                Text = $"Bill #{x.b.BillId} - {x.b.Connection?.ConnectionNumber} - {x.b.Customer?.FullName} ({x.b.BillingMonth}) - balance ${x.due:N2}"
            })
            .ToList();
    }
}
