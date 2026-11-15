using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Data;
using NexusCommunicationSystem.Models;

namespace NexusCommunicationSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public AdminController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // =============================== AUTH ===============================
        [AllowAnonymous, HttpGet]
        public IActionResult Login()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Dashboard");
            return View();
        }

        // Admin login: employee account first, then the old appsettings login
        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, bool remember)
        {
            // 1) an employee with role Admin (user id + password set by the Admin under Employees)
            var employee = await StaffAuth.ValidateAsync(_db, username, password, StaffRoles.Admin);
            if (employee != null)
            {
                await StaffAuth.SignInAsync(HttpContext, employee, StaffRoles.Admin, remember);
                return RedirectToAction("Dashboard");
            }

            // 2) the built-in administrator account from appsettings.json
            var validUser = _config["AdminCredentials:Username"];
            var validPass = _config["AdminCredentials:Password"];

            if (username == validUser && password == validPass)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Admin")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = remember });
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Wrong username or password. Please try again.";
            ViewBag.Username = username;
            return View();
        }

        // Sign out the admin
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // =============================== DASHBOARD ===============================
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.EmployeeCount = await _db.Employees.CountAsync();
            ViewBag.ShopCount = await _db.RetailShops.CountAsync();
            ViewBag.VendorCount = await _db.Vendors.CountAsync();
            ViewBag.PlanCount = await _db.Plans.CountAsync();
            ViewBag.EquipmentCount = await _db.Equipments.SumAsync(x => (int?)x.Quantity) ?? 0;
            ViewBag.ActivePlans = await _db.Plans.CountAsync(x => x.IsActive);
            ViewBag.RecentEmployees = await _db.Employees.OrderByDescending(x => x.Id).Take(5).ToListAsync();
            ViewBag.RecentPlans = await _db.Plans.OrderByDescending(x => x.Id).Take(5).ToListAsync();
            return View();
        }


        // =============================== Employees ===============================
        [ActionName("Employees")]
        public async Task<IActionResult> EmployeesList(string q)
        {
            var query = _db.Employees.Include(x => x.RetailShop).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.FullName.Contains(q) || x.Email.Contains(q) || x.Designation.Contains(q)
                                      || x.Phone.Contains(q) || x.UserId.Contains(q) || x.Role.Contains(q));
            }
            ViewBag.Q = q;
            return View("Employees", await query.OrderByDescending(x => x.Id).ToListAsync());
        }

        // Show the empty employee form
        [HttpGet]
        public async Task<IActionResult> AddEmployee()
        {
            await LoadShops();
            return View(new Employee { JoinDate = DateTime.Today });
        }

        // Save a new employee (password is stored hashed)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(Employee model)
        {
            await ValidateStaff(model, isNew: true);
            if (!ModelState.IsValid)
            {
                await LoadShops();
                return View(model);
            }

            model.UserId = model.UserId.Trim();
            model.PasswordHash = StaffAuth.Hash(model, model.Password);
            model.Password = null;
            if (model.Role != StaffRoles.Retail) model.RetailShopId = null;

            _db.Employees.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Employee added successfully.";
            return RedirectToAction("Employees");
        }

        // Load an employee for editing
        [HttpGet]
        public async Task<IActionResult> EditEmployee(int id)
        {
            var item = await _db.Employees.FindAsync(id);
            if (item == null) return NotFound();
            await LoadShops();
            return View(item);
        }

        // Save employee changes; a blank password keeps the old one
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(Employee model)
        {
            var item = await _db.Employees.FindAsync(model.Id);
            if (item == null) return NotFound();

            await ValidateStaff(model, isNew: false);
            if (!ModelState.IsValid)
            {
                await LoadShops();
                return View(model);
            }

            // Copy only the form fields: the stored password hash must never be overwritten by the form
            item.FullName = model.FullName;
            item.Email = model.Email;
            item.Phone = model.Phone;
            item.Designation = model.Designation;
            item.Salary = model.Salary;
            item.JoinDate = model.JoinDate;
            item.IsActive = model.IsActive;
            item.Role = model.Role;
            item.UserId = model.UserId.Trim();
            item.RetailShopId = model.Role == StaffRoles.Retail ? model.RetailShopId : null;

            // Password box left empty = keep the old password
            if (!string.IsNullOrWhiteSpace(model.Password))
                item.PasswordHash = StaffAuth.Hash(item, model.Password);

            await _db.SaveChangesAsync();
            TempData["Success"] = "Employee updated successfully.";
            return RedirectToAction("Employees");
        }

        // Delete an employee unless he has placed orders
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var item = await _db.Employees.FindAsync(id);
            if (item != null)
            {
                if (await _db.Orders.AnyAsync(o => o.PlacedByEmployeeId == id))
                {
                    TempData["Error"] = "This employee has placed customer orders, so he cannot be deleted. Untick Active to block his login instead.";
                    return RedirectToAction("Employees");
                }
                _db.Employees.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Employee deleted successfully.";
            }
            return RedirectToAction("Employees");
        }


        // =============================== RetailShops ===============================
        [ActionName("RetailShops")]
        public async Task<IActionResult> RetailShopsList(string q)
        {
            var query = _db.RetailShops.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.ShopName.Contains(q) || x.OwnerName.Contains(q) || x.City.Contains(q) || x.Phone.Contains(q));
            }
            ViewBag.Q = q;
            return View("RetailShops", await query.OrderByDescending(x => x.Id).ToListAsync());
        }

        // Show the empty retail shop form
        [HttpGet]
        public async Task<IActionResult> AddRetailShop()
        {
            return View(new RetailShop());
        }

        // Save a new retail shop
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRetailShop(RetailShop model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _db.RetailShops.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Retail Shop added successfully.";
            return RedirectToAction("RetailShops");
        }

        // Load a retail shop for editing
        [HttpGet]
        public async Task<IActionResult> EditRetailShop(int id)
        {
            var item = await _db.RetailShops.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // Save retail shop changes
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRetailShop(RetailShop model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _db.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Retail Shop updated successfully.";
            return RedirectToAction("RetailShops");
        }

        // Delete a shop only when it has no employees
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRetailShop(int id)
        {
            var item = await _db.RetailShops.FindAsync(id);
            if (item != null)
            {
                if (await _db.Employees.AnyAsync(e => e.RetailShopId == id))
                {
                    TempData["Error"] = "Employees are linked to this retail shop, so it cannot be deleted. Untick Active instead, or move the employees to another shop first.";
                    return RedirectToAction("RetailShops");
                }
                _db.RetailShops.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Retail Shop deleted successfully.";
            }
            return RedirectToAction("RetailShops");
        }


        // =============================== Vendors ===============================
        [ActionName("Vendors")]
        public async Task<IActionResult> VendorsList(string q)
        {
            var query = _db.Vendors.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.VendorName.Contains(q) || x.ContactPerson.Contains(q) || x.Phone.Contains(q));
            }
            ViewBag.Q = q;
            return View("Vendors", await query.OrderByDescending(x => x.Id).ToListAsync());
        }

        // Show the empty vendor form
        [HttpGet]
        public async Task<IActionResult> AddVendor()
        {
            return View(new Vendor());
        }

        // Save a new vendor
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVendor(Vendor model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _db.Vendors.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vendor added successfully.";
            return RedirectToAction("Vendors");
        }

        // Load a vendor for editing
        [HttpGet]
        public async Task<IActionResult> EditVendor(int id)
        {
            var item = await _db.Vendors.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // Save vendor changes
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVendor(Vendor model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _db.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vendor updated successfully.";
            return RedirectToAction("Vendors");
        }

        // Delete a vendor
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            var item = await _db.Vendors.FindAsync(id);
            if (item != null)
            {
                // purchase lines keep their vendor, so a vendor with purchase history cannot be deleted
                if (await _db.PurchaseOrders.AnyAsync(p => p.VendorId == id))
                {
                    TempData["Error"] = "This vendor has purchase orders, so it cannot be deleted. Set the vendor to inactive instead.";
                    return RedirectToAction("Vendors");
                }
                _db.Vendors.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Vendor deleted successfully.";
            }
            return RedirectToAction("Vendors");
        }


        // =============================== Plans ===============================
        [ActionName("Plans")]
        public async Task<IActionResult> PlansList(string q)
        {
            var query = _db.Plans.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.PlanName.Contains(q) || x.PlanType.Contains(q));
            }
            ViewBag.Q = q;
            return View("Plans", await query.OrderByDescending(x => x.Id).ToListAsync());
        }

        // Show the empty plan form
        [HttpGet]
        public async Task<IActionResult> AddPlan()
        {
            return View(new Plan());
        }

        // Save a new plan
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPlan(Plan model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _db.Plans.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Plan added successfully.";
            return RedirectToAction("Plans");
        }

        // Load a plan for editing
        [HttpGet]
        public async Task<IActionResult> EditPlan(int id)
        {
            var item = await _db.Plans.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // Save plan changes
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPlan(Plan model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _db.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Plan updated successfully.";
            return RedirectToAction("Plans");
        }

        // Delete a plan
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var item = await _db.Plans.FindAsync(id);
            if (item != null)
            {
                _db.Plans.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Plan deleted successfully.";
            }
            return RedirectToAction("Plans");
        }


        // =============================== Purchase list ===============================
        // Equipment ordered from vendors, optionally for a customer order. Received goods go into the stock.

        // Customer orders that still wait for a connection (they will need equipment)
        private IQueryable<Order> OrdersNeedingEquipment() =>
            _db.Orders.Where(o => o.Connection == null && o.Status == "Processing");

        // Purchase list with the demand and stock summary on top
        [HttpGet]
        public async Task<IActionResult> PurchaseList(string status)
        {
            var query = _db.PurchaseOrders.Include(p => p.Vendor).Include(p => p.CustomerOrder).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                query = query.Where(p => p.Status == status);

            // Demand from customer orders against the stock that is available
            var demand = await OrdersNeedingEquipment()
                .GroupBy(o => o.ConnectionType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.Demand = demand.ToDictionary(d => d.Type ?? "-", d => d.Count);
            ViewBag.StockAvailable = await _db.Equipments
                .Where(e => e.Status == "Available").SumAsync(e => (int?)e.Quantity) ?? 0;
            ViewBag.OnOrder = await _db.PurchaseOrders
                .Where(p => p.Status == "Ordered").SumAsync(p => (int?)p.Quantity) ?? 0;
            ViewBag.Status = string.IsNullOrWhiteSpace(status) ? "All" : status;

            return View(await query.OrderByDescending(p => p.Id).ToListAsync());
        }

        // Show the new purchase form
        [HttpGet]
        public async Task<IActionResult> AddPurchase(int? orderId)
        {
            await LoadPurchaseLists();
            return View(new PurchaseOrder { CustomerOrderId = orderId });
        }

        // Save a new purchase line and give it a PO number
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPurchase(PurchaseOrder model)
        {
            ModelState.Remove(nameof(model.PurchaseNumber));
            ModelState.Remove(nameof(model.Status));
            if (model.ExpectedDate.HasValue && model.ExpectedDate.Value.Date < model.OrderDate.Date)
                ModelState.AddModelError(nameof(model.ExpectedDate), "Expected date cannot be before the order date.");
            if (!ModelState.IsValid)
            {
                await LoadPurchaseLists();
                return View(model);
            }

            model.TotalAmount = model.Quantity * model.UnitPrice;
            model.Status = "Ordered";
            _db.PurchaseOrders.Add(model);
            await _db.SaveChangesAsync();                       // gets the Id
            model.PurchaseNumber = "PO-" + model.Id.ToString("D6");
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Purchase {model.PurchaseNumber} added to the purchase list.";
            return RedirectToAction("PurchaseList");
        }

        // Goods arrived: close the purchase and add the quantity to stock
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceivePurchase(int id)
        {
            var po = await _db.PurchaseOrders.FindAsync(id);
            if (po == null || po.Status != "Ordered")
            {
                TempData["Error"] = "Only an order that is still waiting can be received.";
                return RedirectToAction("PurchaseList");
            }

            // Same item from the same vendor already in stock: add to it, otherwise create a new stock line
            var stock = await _db.Equipments.FirstOrDefaultAsync(e =>
                e.EquipmentName == po.EquipmentName && e.EquipmentType == po.EquipmentType &&
                e.VendorId == po.VendorId && e.Status == "Available" &&
                (e.SerialNumber == null || e.SerialNumber == ""));
            if (stock != null)
            {
                stock.Quantity += po.Quantity;
            }
            else
            {
                _db.Equipments.Add(new Equipment
                {
                    EquipmentName = po.EquipmentName,
                    EquipmentType = po.EquipmentType,
                    Quantity = po.Quantity,
                    UnitPrice = po.UnitPrice,
                    Status = "Available",
                    Location = "Main warehouse",
                    PurchaseDate = DateTime.Today,
                    VendorId = po.VendorId
                });
            }

            po.Status = "Received";
            po.ReceivedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{po.PurchaseNumber} received. {po.Quantity} x {po.EquipmentName} added to the equipment stock.";
            return RedirectToAction("PurchaseList");
        }

        // Cancel a purchase that is still waiting
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelPurchase(int id)
        {
            var po = await _db.PurchaseOrders.FindAsync(id);
            if (po != null && po.Status == "Ordered")
            {
                po.Status = "Cancelled";
                await _db.SaveChangesAsync();
                TempData["Success"] = $"{po.PurchaseNumber} cancelled.";
            }
            return RedirectToAction("PurchaseList");
        }

        // Fill the vendor and customer order dropdowns
        private async Task LoadPurchaseLists()
        {
            ViewBag.Vendors = new SelectList(
                await _db.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync(), "Id", "VendorName");

            var orders = await OrdersNeedingEquipment().OrderByDescending(o => o.OrderDate).Take(200).ToListAsync();
            ViewBag.CustomerOrders = orders.Select(o => new SelectListItem
            {
                Value = o.Id.ToString(),
                Text = $"{o.OrderNumber} - {o.ConnectionType}, {o.City}"
            }).ToList();
        }

        // =============================== Equipments ===============================
        [ActionName("Equipment")]
        public async Task<IActionResult> EquipmentList(string q)
        {
            var query = _db.Equipments.Include(x => x.Vendor).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.EquipmentName.Contains(q) || x.EquipmentType.Contains(q) || x.SerialNumber.Contains(q));
            }
            ViewBag.Q = q;
            return View("Equipment", await query.OrderByDescending(x => x.Id).ToListAsync());
        }

        // Show the empty equipment form
        [HttpGet]
        public async Task<IActionResult> AddEquipment()
        {
            await LoadVendors();
            return View(new Equipment());
        }

        // Save new equipment
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEquipment(Equipment model)
        {
            if (!ModelState.IsValid)
            {
                await LoadVendors();
                return View(model);
            }
            _db.Equipments.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Equipment added successfully.";
            return RedirectToAction("Equipment");
        }

        // Load equipment for editing
        [HttpGet]
        public async Task<IActionResult> EditEquipment(int id)
        {
            var item = await _db.Equipments.FindAsync(id);
            if (item == null) return NotFound();
            await LoadVendors();
            return View(item);
        }

        // Save equipment changes
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEquipment(Equipment model)
        {
            if (!ModelState.IsValid)
            {
                await LoadVendors();
                return View(model);
            }
            _db.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Equipment updated successfully.";
            return RedirectToAction("Equipment");
        }

        // Delete equipment
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            var item = await _db.Equipments.FindAsync(id);
            if (item != null)
            {
                if (await _db.EquipmentAssignments.AnyAsync(a => a.EquipmentId == id))
                {
                    TempData["Error"] = "This equipment is assigned to a connection, so it cannot be deleted. Set its status to Damaged / Under Repair instead.";
                    return RedirectToAction("Equipment");
                }
                _db.Equipments.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Equipment deleted successfully.";
            }
            return RedirectToAction("Equipment");
        }

        // =============================== HELPERS ===============================
        // Checks that are not plain data annotations: role, unique user id, password on new employees, shop for Retail
        private async Task ValidateStaff(Employee model, bool isNew)
        {
            if (!StaffRoles.All.Contains(model.Role ?? ""))
                ModelState.AddModelError(nameof(model.Role), "Please choose a valid role.");

            if (!string.IsNullOrWhiteSpace(model.UserId))
            {
                var uid = model.UserId.Trim();
                if (await _db.Employees.AnyAsync(e => e.UserId == uid && e.Id != model.Id))
                    ModelState.AddModelError(nameof(model.UserId), "This User ID is already used by another employee.");
            }

            if (isNew && string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "Password is required for a new employee.");

            if (model.Role == StaffRoles.Retail)
            {
                if (!model.RetailShopId.HasValue || !await _db.RetailShops.AnyAsync(s => s.Id == model.RetailShopId.Value))
                    ModelState.AddModelError(nameof(model.RetailShopId), "A Retail employee must belong to a retail shop.");
            }

            // these navigation / hash fields are not part of the form
            ModelState.Remove(nameof(model.PasswordHash));
            ModelState.Remove(nameof(model.RetailShop));
        }

        // Fill the retail shop dropdown
        private async Task LoadShops()
        {
            var shops = await _db.RetailShops.Where(s => s.IsActive).OrderBy(s => s.ShopName).ToListAsync();
            ViewBag.Shops = new SelectList(shops, "Id", "ShopName");
        }

        // Fill the vendor dropdown
        private async Task LoadVendors()
        {
            var vendors = await _db.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Vendors = new SelectList(vendors, "Id", "VendorName");
        }
    }
}