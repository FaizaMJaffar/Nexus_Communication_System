using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Data;
using NexusCommunicationSystem.Models;
using FeedbackEntity = NexusCommunicationSystem.Models.Feedback;

namespace NexusCommunicationSystem.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<Customer> _hasher = new();

    public CustomerController(ApplicationDbContext db)
    {
        _db = db;
    }

    private int CurrentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    // Log the customer in by writing the auth cookie
    private async Task SignInCustomer(Customer c, bool persistent)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, c.Id.ToString()),
            new Claim(ClaimTypes.Name, c.FullName),
            new Claim(ClaimTypes.Email, c.Email),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = persistent });
    }

    // ===================== REGISTER =====================
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.IsInRole("Customer"))
            return RedirectToAction(nameof(Dashboard));
        return View(new RegisterViewModel());
    }

    // Create the account and sign the customer in
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim().ToLower();
        if (await _db.Customers.AnyAsync(c => c.Email == email))
        {
            ModelState.AddModelError("Email", "An account with this email already exists.");
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

        TempData["Success"] = "Account created successfully! Please log in.";
        return RedirectToAction(nameof(Login));
    }

    // ===================== LOGIN / LOGOUT =====================
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        if (User.IsInRole("Customer"))
            return RedirectToAction(nameof(Dashboard));
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // Log in with email and password
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim().ToLower();
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == email);

        if (customer == null ||
            _hasher.VerifyHashedPassword(customer, customer.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInCustomer(customer, model.RememberMe);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction(nameof(Dashboard));
    }

    // Sign out the customer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    // ===================== DASHBOARD =====================
    public async Task<IActionResult> Dashboard()
    {
        var id = CurrentId;
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        var unpaid = await _db.Orders
            .Where(o => o.CustomerId == id && o.PaymentStatus == "Unpaid")
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            Customer = customer,
            TotalOrders = await _db.Orders.CountAsync(o => o.CustomerId == id),
            ActiveConnections = await _db.Connections.CountAsync(c => c.CustomerId == id && c.Status == "Active"),
            PendingConnections = await _db.Orders.CountAsync(o => o.CustomerId == id && o.Status == "Processing"),
            PendingBills = unpaid.Sum(o => o.TotalAmount),
            RecentOrders = await _db.Orders
                .Where(o => o.CustomerId == id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync()
        };
        return View(vm);
    }

    // ===================== PROFILE =====================
    public async Task<IActionResult> Profile()
    {
        var customer = await _db.Customers.FindAsync(CurrentId);
        if (customer == null) return RedirectToAction(nameof(Login));
        return View(customer);
    }

    // Show the profile edit form
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var c = await _db.Customers.FindAsync(CurrentId);
        if (c == null) return RedirectToAction(nameof(Login));

        return View(new EditProfileViewModel
        {
            FullName = c.FullName,
            Email = c.Email,
            Phone = c.Phone,
            City = c.City,
            Address = c.Address
        });
    }

    // Save profile changes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        var c = await _db.Customers.FindAsync(CurrentId);
        if (c == null) return RedirectToAction(nameof(Login));
        model.Email = c.Email;

        bool changingPassword = !string.IsNullOrWhiteSpace(model.NewPassword);
        if (changingPassword)
        {
            if (string.IsNullOrEmpty(model.CurrentPassword) ||
                _hasher.VerifyHashedPassword(c, c.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            }
        }

        if (!ModelState.IsValid) return View(model);

        c.FullName = model.FullName.Trim();
        c.Phone = model.Phone.Trim();
        c.City = model.City;
        c.Address = model.Address.Trim();
        if (changingPassword)
            c.PasswordHash = _hasher.HashPassword(c, model.NewPassword);

        await _db.SaveChangesAsync();

        var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SignInCustomer(c, auth?.Properties?.IsPersistent ?? false);

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    // ===================== APPLY CONNECTION =====================
    [HttpGet]
    public async Task<IActionResult> ApplyConnection(string type = null)
    {
        var c = await _db.Customers.FindAsync(CurrentId);
        var vm = new ApplyConnectionViewModel
        {
            InstallAddress = c?.Address,
            City = c?.City
        };
        if (!string.IsNullOrEmpty(type) && PlanCatalog.Types.Contains(type))
            vm.ConnectionType = type;
        return View(vm);
    }

    // Step 1 of a new connection: type and address
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApplyConnection(ApplyConnectionViewModel model)
    {
        if (!PlanCatalog.Types.Contains(model.ConnectionType ?? ""))
            ModelState.AddModelError("ConnectionType", "Please select a valid connection type.");

        if (!ModelState.IsValid) return View(model);

        return RedirectToAction(nameof(SelectPlan), new
        {
            type = model.ConnectionType,
            address = model.InstallAddress,
            city = model.City
        });
    }

    // ===================== SELECT PLAN =====================
    [HttpGet]
    public async Task<IActionResult> SelectPlan(string type, string address, string city)
    {
        if (string.IsNullOrEmpty(type) || !PlanCatalog.Types.Contains(type) ||
            string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(city))
        {
            return RedirectToAction(nameof(ApplyConnection));
        }

        return View(new SelectPlanViewModel
        {
            ConnectionType = type,
            InstallAddress = address,
            City = city,
            Plans = await LoadPlans(type)
        });
    }

    // Step 2: pick a plan and create the order
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPlan(SelectPlanViewModel model)
    {
        model.Plans = await LoadPlans(model.ConnectionType ?? "");

        var plan = model.PlanId.HasValue
            ? model.Plans.FirstOrDefault(p => p.Id == model.PlanId.Value)
            : null;

        if (model.PlanId.HasValue && plan == null)
            ModelState.AddModelError("PlanId", "Invalid plan selected.");

        if (!ModelState.IsValid || plan == null) return View(model);

        var now = DateTime.Now;

        // Order id: type letter + 10-digit serial (D / B / T), e.g. D0000000001
        var typeCode = NexusIds.TypeCode(plan.ConnectionType);
        var existingNumbers = await _db.Orders
            .Where(o => o.OrderNumber.StartsWith(typeCode))
            .Select(o => o.OrderNumber)
            .ToListAsync();
        var deposit = PlanCatalog.DepositFor(plan.ConnectionType);

        // The order goes to the Technical team (feasibility check). The connection itself is
        // created by them once the area is feasible, so no Connection is created here.
        // service tax is charged on the whole first invoice (plan + deposit)
        var tax = BillingRules.Round((plan.Price + deposit) * BillingRules.ServiceTaxPercent / 100m);
        var order = new Order
        {
            OrderNumber = NexusIds.NextOrderNumber(existingNumbers, typeCode),
            CustomerId = CurrentId,
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
            DueDate = now.AddDays(7)
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(OrderConfirmation), new { id = order.Id });
    }

    // Fill the plan list for the chosen connection type
    private Task<List<Plan>> LoadPlans(string type) =>
        _db.Plans.Where(p => p.IsActive && p.ConnectionType == type)
            .OrderBy(p => p.Price)
            .ToListAsync();

    // ===================== ORDERS =====================
    public async Task<IActionResult> OrderConfirmation(int id)
    {
        var order = await _db.Orders.Include(o => o.Connection)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == CurrentId);
        if (order == null) return RedirectToAction(nameof(MyOrders));
        return View(order);
    }

    // List of this customer's orders
    public async Task<IActionResult> MyOrders()
    {
        var orders = await _db.Orders
            .Where(o => o.CustomerId == CurrentId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return View(orders);
    }

    // Status by Order ID or Account ID, limited to this customer's own orders
    [HttpGet]
    public async Task<IActionResult> TrackStatus(string q)
    {
        var mine = _db.Orders.Where(o => o.CustomerId == CurrentId);
        return View(await StatusLookup.FindAsync(_db, mine, q));
    }

    // Details of one order (only the customer's own)
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _db.Orders.Include(o => o.Connection).Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == CurrentId);
        if (order == null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction(nameof(MyOrders));
        }
        return View(order);
    }

    // ===================== CONNECTIONS =====================
    public async Task<IActionResult> MyConnections()
    {
        var list = await _db.Connections
            .Where(c => c.CustomerId == CurrentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(list);
    }

    // ===================== BILLS =====================
    public async Task<IActionResult> MyBills()
    {
        var bills = await _db.Orders
            .Where(o => o.CustomerId == CurrentId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        ViewBag.TotalUnpaid = bills.Where(b => b.PaymentStatus == "Unpaid").Sum(b => b.TotalAmount);
        ViewBag.TotalPaid = bills.Where(b => b.PaymentStatus == "Paid").Sum(b => b.TotalAmount);

        // Monthly bills generated by the Accounts department (read-only for the customer)
        ViewBag.AccountBills = await _db.Bills
            .Include(b => b.Payments)
            .Include(b => b.Connection)
            .Where(b => b.CustomerId == CurrentId)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync();
        return View(bills);
    }

    // Pay an unpaid order invoice
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayBill(int id)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == CurrentId);

        if (order == null || order.PaymentStatus == "Paid")
        {
            TempData["Error"] = "This bill cannot be paid.";
            return RedirectToAction(nameof(MyBills));
        }

        order.PaymentStatus = "Paid";
        order.PaidDate = DateTime.Now;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Payment for order {order.OrderNumber} received. Thank you!";
        return RedirectToAction(nameof(MyBills));
    }

    // ===================== FEEDBACK =====================
    [HttpGet]
    public async Task<IActionResult> Feedback()
    {
        ViewBag.Previous = await LoadFeedbacks();
        return View(new FeedbackEntity());
    }

    // Save the feedback
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback([Bind("Subject,Message,Rating")] FeedbackEntity model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Previous = await LoadFeedbacks();
            return View(model);
        }

        model.CustomerId = CurrentId;
        model.CreatedAt = DateTime.Now;
        _db.Feedbacks.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Thank you for your feedback!";
        return RedirectToAction(nameof(Feedback));
    }

    // Load earlier feedback of this customer
    private Task<List<FeedbackEntity>> LoadFeedbacks() =>
        _db.Feedbacks.Where(f => f.CustomerId == CurrentId)
            .OrderByDescending(f => f.CreatedAt)
            .Take(10)
            .ToListAsync();
}
