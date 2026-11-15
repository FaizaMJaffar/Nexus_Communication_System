#nullable enable
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Data;
using NexusCommunicationSystem.Models;
using InventoryItem = NexusCommunicationSystem.Models.Equipment;

namespace NexusCommunicationSystem.Controllers
{
    [Authorize(Roles = "Technical")]
    public class TechnicalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public TechnicalController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Show the pending work order count in the header
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (User.IsInRole("Technical"))
            {
                ViewData["StaffName"] = User.Identity?.Name ?? "Technical Staff";
                ViewData["QueueCount"] = _context.Orders.Count(o =>
                    o.Status == "Processing" &&
                    !o.Feasibilities.Any(f => f.Status != "Pending"));
            }
            base.OnActionExecuting(context);
        }

        // Newest feasibility check of an order
        private static Feasibility? Latest(Order o) =>
            o.Feasibilities?.OrderByDescending(f => f.FeasibilityId).FirstOrDefault();

        // ===================== LOGIN =====================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.IsInRole("Technical")) return RedirectToAction("Dashboard");
            return View();
        }

        // Technical staff login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1) an employee with role Technical (user id + password set by the Admin under Employees)
            var employee = await StaffAuth.ValidateAsync(_context, username, password, StaffRoles.Technical);
            if (employee != null)
            {
                await StaffAuth.SignInAsync(HttpContext, employee, StaffRoles.Technical);
                return RedirectToAction("Dashboard");
            }

            // 2) the built-in account from appsettings.json
            var expectedUser = _config["Technical:Username"] ?? "technical";
            var expectedPass = _config["Technical:Password"] ?? "Tech@123";

            if (username == expectedUser && password == expectedPass)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Technical Staff"),
                    new Claim(ClaimTypes.Role, "Technical")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        // Sign out the technical employee
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ===================== DASHBOARD =====================
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.PendingFeasibility = ViewData["QueueCount"] ?? 0;
            ViewBag.AwaitingSplicing = await _context.Connections.CountAsync(c => c.Status == "Pending");
            ViewBag.ActiveConnections = await _context.Connections.CountAsync(c => c.Status == "Active");
            ViewBag.InactiveConnections = await _context.Connections.CountAsync(c => c.Status == "Temporarily Inactive" || c.Status == "Permanently Inactive");

            var recentFeasibilities = await _context.Feasibilities
                .OrderByDescending(f => f.CheckedDate)
                .Take(5)
                .ToListAsync();

            return View(recentFeasibilities);
        }

        // ===================== WORK ORDERS (real customer orders) =====================
        public async Task<IActionResult> Orders(string? status)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Connection)
                .Include(o => o.Feasibilities)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var rows = orders.Select(o =>
            {
                var f = Latest(o);
                return new WorkOrderRow
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.Customer?.FullName ?? "-",
                    ConnectionType = o.ConnectionType,
                    PlanName = o.PlanName,
                    City = o.City,
                    FeasibilityStatus = f?.Status ?? "Pending",
                    FeasibilityId = f?.FeasibilityId,
                    CheckedBy = f?.CheckedBy,
                    ConnectionId = o.Connection?.Id
                };
            }).ToList();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                rows = rows.Where(r => r.FeasibilityStatus == status).ToList();
            }

            ViewBag.CurrentStatus = string.IsNullOrEmpty(status) ? "All" : status;
            return View(rows);
        }

        // id = Order.Id
        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Connection)
                .Include(o => o.Feasibilities)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(new WorkOrderDetails { Order = order, Feasibility = Latest(order) });
        }

        // id = Feasibility.FeasibilityId
        [HttpGet]
        public async Task<IActionResult> UpdateOrderStatus(int? id)
        {
            if (id == null) return NotFound();

            var feasibility = await _context.Feasibilities.FirstOrDefaultAsync(f => f.FeasibilityId == id);
            if (feasibility == null) return NotFound();

            return View(feasibility);
        }

        // Change the status of a feasibility check by hand
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status, string? remarks)
        {
            var feasibility = await _context.Feasibilities
                .Include(f => f.Order)
                .FirstOrDefaultAsync(f => f.FeasibilityId == id);

            if (feasibility == null) return NotFound();

            if (status != "Pending" && status != "Feasible" && status != "Rejected")
            {
                return RedirectToAction("UpdateOrderStatus", new { id });
            }

            feasibility.Status = status;
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                feasibility.Remarks = remarks;
            }
            feasibility.CheckedDate = DateTime.Now;
            SyncOrderStatus(feasibility.Order, status);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"WO-{feasibility.OrderId} updated to {status}.";
            return RedirectToAction("OrderDetails", new { id = feasibility.OrderId });
        }

        // A rejected survey rejects the customer's order; anything else keeps it in progress.
        private static void SyncOrderStatus(Order? order, string feasibilityStatus)
        {
            if (order == null || order.Status == "Completed") return;
            order.Status = feasibilityStatus == "Rejected" ? "Rejected" : "Processing";
        }

        // ===================== FEASIBILITY CHECK =====================
        [HttpGet]
        public async Task<IActionResult> FeasibilityCheck(int? orderId)
        {
            var model = new Feasibility();
            if (orderId.HasValue) model.OrderId = orderId.Value;

            await LoadOrderDropdown();
            return View(model);
        }

        // Save the feasibility result of an order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FeasibilityCheck(Feasibility model, string decision)
        {
            ModelState.Remove(nameof(model.Status));

            var order = await _context.Orders.Include(o => o.Connection)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId);
            if (order == null)
            {
                ModelState.AddModelError(nameof(model.OrderId), "Please select a work order.");
            }
            else if (order.Connection != null)
            {
                ModelState.AddModelError(nameof(model.OrderId), "A connection already exists for this order.");
            }

            if (!ModelState.IsValid || order == null)
            {
                await LoadOrderDropdown();
                return View(model);
            }

            // Does the customer already hold an Active landline of this company?
            model.HasExistingLandline = await CustomerHasLandline(order.CustomerId);

            var needLandline = FeasibilityRules.NeedsLandlineCheck(order.ConnectionType, model.HasExistingLandline);
            var needInternet = FeasibilityRules.NeedsInternetCheck(order.ConnectionType);

            // Checks that do not apply to this order are not stored
            if (!needLandline) model.LandlineAvailable = null;
            if (!needInternet) model.InternetAvailable = null;

            if (decision == "Pending")
            {
                model.Status = "Pending";
            }
            else
            {
                // Every check that applies must be answered before a decision can be made
                if (needLandline && model.LandlineAvailable == null)
                    ModelState.AddModelError(nameof(model.LandlineAvailable), "Select the landline availability.");
                if (needInternet && model.InternetAvailable == null)
                    ModelState.AddModelError(nameof(model.InternetAvailable), "Select the internet availability.");
                if (!ModelState.IsValid)
                {
                    await LoadOrderDropdown();
                    return View(model);
                }

                // The result follows the specification rules; the buttons only confirm it
                model.Status = FeasibilityRules.IsFeasible(order.ConnectionType, model.HasExistingLandline,
                    model.LandlineAvailable, model.InternetAvailable, model.ServerAvailable)
                    ? "Feasible" : "Rejected";
            }

            model.CheckedBy = User.Identity?.Name ?? "Technical Staff";
            model.CheckedDate = DateTime.Now;

            _context.Feasibilities.Add(model);
            SyncOrderStatus(order, model.Status);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Feasibility for WO-{model.OrderId} saved as {model.Status}.";
            return RedirectToAction("Orders");
        }

        // The form asks this to know which checks to show for the chosen order (AJAX)
        [HttpGet]
        public async Task<IActionResult> FeasibilityInfo(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();

            var hasLandline = await CustomerHasLandline(order.CustomerId);
            return Json(new
            {
                connectionType = order.ConnectionType,
                hasExistingLandline = hasLandline,
                needLandline = FeasibilityRules.NeedsLandlineCheck(order.ConnectionType, hasLandline),
                needInternet = FeasibilityRules.NeedsInternetCheck(order.ConnectionType)
            });
        }

        // True when the customer already has an Active landline connection with this company
        private Task<bool> CustomerHasLandline(int customerId) =>
            _context.Connections.AnyAsync(c => c.CustomerId == customerId
                && c.ConnectionType == "Landline" && c.Status == "Active");

        // Orders that still need a survey: in progress and without a connection
        private async Task LoadOrderDropdown()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Connection == null && o.Status != "Completed")
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = orders.Select(o => new SelectListItem
            {
                Value = o.Id.ToString(),
                Text = $"WO-{o.Id} / {o.OrderNumber} - {o.Customer?.FullName} ({o.City})"
            }).ToList();
        }

        // ===================== CONNECTIONS =====================
        public async Task<IActionResult> Connections()
        {
            var list = await _context.Connections
                .Include(c => c.Customer)
                .OrderByDescending(c => c.Id)
                .ToListAsync();
            return View(list);
        }

        // Show the create connection form
        [HttpGet]
        public async Task<IActionResult> CreateConnection(int? orderId)
        {
            await LoadFeasibleOrders();
            return View(new CreateConnectionViewModel { OrderId = orderId });
        }

        // Create the connection and its 16 character account id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConnection(CreateConnectionViewModel model)
        {
            Order? order = null;
            if (model.OrderId.HasValue)
            {
                order = await _context.Orders
                    .Include(o => o.Connection)
                    .Include(o => o.Feasibilities)
                    .FirstOrDefaultAsync(o => o.Id == model.OrderId.Value);

                if (order == null)
                    ModelState.AddModelError(nameof(model.OrderId), "Work order not found.");
                else if (order.Connection != null)
                    ModelState.AddModelError(nameof(model.OrderId), "A connection already exists for this order.");
                else if (Latest(order)?.Status != "Feasible")
                    ModelState.AddModelError(nameof(model.OrderId), "This order is not marked Feasible yet.");
            }

            if (!ModelState.IsValid || order == null)
            {
                await LoadFeasibleOrders();
                return View(model);
            }

            // Account id (16 characters): type letter + 3-digit city code + 12-digit serial, e.g. B101000000000001
            var typeCode = NexusIds.TypeCode(order.ConnectionType);
            var existingNumbers = await _context.Connections
                .Where(c => c.ConnectionNumber.StartsWith(typeCode))
                .Select(c => c.ConnectionNumber)
                .ToListAsync();
            var number = NexusIds.NextAccountNumber(existingNumbers, typeCode, NexusIds.CityCode(order.City));

            var connection = new Connection
            {
                ConnectionNumber = number,
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                PlanId = order.PlanId,
                ConnectionType = order.ConnectionType,
                PlanName = order.PlanName,
                Speed = order.Speed,
                MonthlyCharges = order.MonthlyCharges,
                InstallAddress = order.InstallAddress,
                City = order.City,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                Remarks = model.Remarks
            };

            _context.Connections.Add(connection);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Connection created. Account ID: {connection.ConnectionNumber} (WO-{order.Id}).";
            return RedirectToAction("Connections");
        }

        // Feasible orders that do not have a connection yet
        private async Task LoadFeasibleOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Feasibilities)
                .Where(o => o.Connection == null)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = orders
                .Where(o => Latest(o)?.Status == "Feasible")
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = $"WO-{o.Id} / {o.OrderNumber} - {o.Customer?.FullName} ({o.PlanName})"
                }).ToList();
        }

        // Show the activate form
        [HttpGet]
        public async Task<IActionResult> ActivateConnection(int? id)
        {
            if (id == null) return NotFound();

            var connection = await _context.Connections.FirstOrDefaultAsync(c => c.Id == id);
            if (connection == null) return NotFound();

            if (connection.Status == "Permanently Inactive")
            {
                TempData["Error"] = $"{connection.ConnectionNumber} is permanently inactive and cannot be activated again.";
                return RedirectToAction("Connections");
            }

            return View(connection);
        }

        // Activate a connection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateConnection(int id)
        {
            var connection = await _context.Connections
                .Include(c => c.Order)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (connection == null) return NotFound();

            // a permanently inactive connection is closed for good
            if (connection.Status == "Permanently Inactive")
            {
                TempData["Error"] = $"{connection.ConnectionNumber} is permanently inactive and cannot be activated again.";
                return RedirectToAction("Connections");
            }

            connection.Status = "Active";
            connection.ActivatedAt ??= DateTime.Now;   // keep the first activation date when re-activating
            connection.DeactivatedAt = null;

            // the customer's order is fulfilled once the line is live
            if (connection.Order != null && connection.Order.Status != "Rejected")
                connection.Order.Status = "Completed";

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Connection {connection.ConnectionNumber} is now Active.";
            return RedirectToAction("Connections");
        }

        // Make a connection temporarily or permanently inactive
        [HttpGet]
        public async Task<IActionResult> DeactivateConnection(int? id)
        {
            if (id == null) return NotFound();

            var connection = await _context.Connections.FirstOrDefaultAsync(c => c.Id == id);
            if (connection == null) return NotFound();

            if (connection.Status != "Active" && connection.Status != "Temporarily Inactive")
            {
                TempData["Error"] = $"{connection.ConnectionNumber} is {connection.Status} and cannot be made inactive.";
                return RedirectToAction("Connections");
            }

            return View(connection);
        }

        // inactiveType: "Temporary" or "Permanent" (specification: a connection can be made inactive both ways)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateConnection(int id, string inactiveType, string? reason)
        {
            var connection = await _context.Connections.FirstOrDefaultAsync(c => c.Id == id);
            if (connection == null) return NotFound();

            if (connection.Status != "Active" && connection.Status != "Temporarily Inactive")
            {
                TempData["Error"] = $"{connection.ConnectionNumber} is {connection.Status} and cannot be made inactive.";
                return RedirectToAction("Connections");
            }

            bool permanent = inactiveType == "Permanent";
            connection.Status = permanent ? "Permanently Inactive" : "Temporarily Inactive";
            connection.DeactivatedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                connection.Remarks = reason.Trim();
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Connection {connection.ConnectionNumber} is now {connection.Status}.";
            return RedirectToAction("Connections");
        }

        // ===================== EQUIPMENT =====================
        public async Task<IActionResult> Equipment()
        {
            var inventory = await _context.Equipments.OrderBy(e => e.EquipmentName).ToListAsync();
            ViewBag.Inventory = inventory;
            ViewBag.EquipmentNames = inventory.ToDictionary(e => e.Id, e => e.EquipmentName);

            var list = await _context.EquipmentAssignments
                .Include(a => a.Connection)
                .OrderByDescending(e => e.AssignedDate)
                .ToListAsync();

            return View(list);
        }

        // Show the assign equipment form
        [HttpGet]
        public async Task<IActionResult> AssignEquipment(int? connectionId)
        {
            var model = new EquipmentAssignment();
            if (connectionId.HasValue) model.ConnectionId = connectionId.Value;

            await LoadDropdowns();
            return View(model);
        }

        // Assign stock equipment to a connection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignEquipment(EquipmentAssignment model)
        {
            ModelState.Remove(nameof(model.EquipmentType));

            var item = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == model.EquipmentId);

            if (item == null)
            {
                ModelState.AddModelError(nameof(model.EquipmentId), "Please select equipment.");
            }
            else if (item.Status != "Available" || item.Quantity < 1)
            {
                ModelState.AddModelError(nameof(model.EquipmentId), "This equipment is not available.");
            }

            if (!await _context.Connections.AnyAsync(c => c.Id == model.ConnectionId))
            {
                ModelState.AddModelError(nameof(model.ConnectionId), "Please select a connection.");
            }

            if (!ModelState.IsValid || item == null)
            {
                await LoadDropdowns();
                return View(model);
            }

            var type = item.EquipmentType ?? item.EquipmentName;
            model.EquipmentType = type.Length > 30 ? type.Substring(0, 30) : type;
            model.AssignedDate = DateTime.Now;

            // take one unit from the stock; the last unit flips the item to "In Use"
            if (item.Quantity > 1) item.Quantity -= 1;
            else item.Status = "In Use";

            _context.EquipmentAssignments.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{item.EquipmentName} assigned to connection #{model.ConnectionId}.";
            return RedirectToAction("Equipment");
        }

        // Fill the connection and equipment dropdowns
        private async Task LoadDropdowns()
        {
            var connections = await _context.Connections
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            ViewBag.Connections = connections
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ConnectionNumber + " (WO-" + c.OrderId + ")"
                })
                .ToList();

            var equipment = await _context.Equipments
                .Where(e => e.Status == "Available" && e.Quantity > 0)
                .OrderBy(e => e.EquipmentName)
                .ToListAsync();

            ViewBag.EquipmentList = equipment
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = e.EquipmentName + " (" + (e.SerialNumber ?? "no serial") + ") x" + e.Quantity
                })
                .ToList();
        }

        // Show the add equipment form
        [HttpGet]
        public IActionResult AddEquipment()
        {
            return View(new InventoryItem());
        }

        // Save new equipment from the technical side
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEquipment(InventoryItem model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Status = "Available";
            _context.Equipments.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{model.EquipmentName} added to inventory.";
            return RedirectToAction("Equipment");
        }
    }
}
