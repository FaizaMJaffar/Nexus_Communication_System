#nullable enable
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Data;
using NexusCommunicationSystem.Models;

namespace NexusCommunicationSystem.Controllers
{
    [Authorize(Roles = "Accounts")]
    [AutoValidateAntiforgeryToken]
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AccountsController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        //Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.IsInRole("Accounts")) return RedirectToAction("Dashboard");
            return View();
        }

        // Accounts staff login: employee account first, then the old appsettings login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1) an employee with role Accounts (user id + password set by the Admin under Employees)
            var employee = await StaffAuth.ValidateAsync(_context, username, password, StaffRoles.Accounts);
            if (employee != null)
            {
                await StaffAuth.SignInAsync(HttpContext, employee, StaffRoles.Accounts);
                return RedirectToAction("Dashboard");
            }

            // 2) the built-in account from appsettings.json
            var expectedUser = _config["Accounts:Username"] ?? "accounts";
            var expectedPass = _config["Accounts:Password"] ?? "Acc@123";

            if (username == expectedUser && password == expectedPass)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Accounts Staff"),
                    new Claim(ClaimTypes.Role, "Accounts")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid username or password.";
            ViewBag.Username = username;
            return View();
        }

        // Sign out and go back to the login page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        //Dashboard
        public IActionResult Dashboard()
        {
            int totalBills = _context.Bills.Count();
            int paidBills = _context.Bills.Where(b => b.Status == "Paid").Count();
            int unpaidBills = _context.Bills.Where(b => b.Status == "Unpaid").Count();
            int partialBills = _context.Bills.Where(b => b.Status == "Partial").Count();
            int overdueBills = _context.Bills.Where(b => b.DueDate < DateTime.Now && b.Status != "Paid").Count();

            decimal totalCollected = 0;
            if (_context.Payments.Any())
            {
                totalCollected = _context.Payments.Sum(p => p.AmountPaid);
            }

            ViewBag.TotalBills = totalBills;
            ViewBag.PaidBills = paidBills;
            ViewBag.UnpaidBills = unpaidBills;
            ViewBag.PartialBills = partialBills;
            ViewBag.OverdueBills = overdueBills;
            ViewBag.TotalCollected = totalCollected;

            return View();
        }

        //GenrateBill
        [HttpGet]
        public IActionResult GenerateBill()
        {
            return View();
        }

        // The bill is calculated automatically (nothing is typed in by hand):
        //   plan charges of the connection, equipment charges and security deposit (first bill only),
        //   bulk discount by the customer's number of connections, then 12.24 % service tax on the whole bill.
        [HttpPost]
        public IActionResult GenerateBill(int customerId, int connectionId, string billingMonth)
        {
            var connection = _context.Connections
                .Include(c => c.Order)
                .FirstOrDefault(c => c.Id == connectionId);
            if (connection == null || connection.CustomerId != customerId)
            {
                ViewBag.Error = "Connection not found for this customer. Check the Customer ID and Connection ID (see the Connections page).";
                return View();
            }

            billingMonth = (billingMonth ?? "").Trim();
            if (_context.Bills.Any(b => b.ConnectionId == connectionId && b.BillingMonth == billingMonth))
            {
                ViewBag.Error = "A bill for this connection and month already exists.";
                return View();
            }

            bool firstBill = !_context.Bills.Any(b => b.ConnectionId == connectionId);

            Bill bill = new Bill();
            bill.CustomerId = customerId;
            bill.ConnectionId = connectionId;
            bill.BillingMonth = billingMonth;
            bill.BillDate = DateTime.Now;
            bill.DueDate = DateTime.Now.AddDays(15);
            bill.Status = "Unpaid";

            // 1) plan charges
            bill.BillingDetails.Add(new BillingDetail
            {
                Description = "Plan charges - " + connection.PlanName,
                Quantity = 1,
                UnitPrice = connection.MonthlyCharges,
                Amount = connection.MonthlyCharges
            });

            // 2) equipment assigned to the connection (charged once, on the first bill)
            if (firstBill)
            {
                var assigned = _context.EquipmentAssignments
                    .Include(a => a.Equipment)
                    .Where(a => a.ConnectionId == connectionId)
                    .ToList();
                foreach (var item in assigned)
                {
                    decimal price = item.Equipment?.UnitPrice ?? 0m;
                    bill.BillingDetails.Add(new BillingDetail
                    {
                        Description = "Equipment - " + (item.Equipment?.EquipmentName ?? item.EquipmentType),
                        Quantity = 1,
                        UnitPrice = price,
                        Amount = price
                    });
                }
            }
            bill.SubTotal = bill.BillingDetails.Sum(d => d.Amount);

            // 3) refundable security deposit by connection type (first bill only; skipped when the
            //    customer already paid it with the order invoice)
            bool depositPaidWithOrder = connection.Order != null && connection.Order.PaymentStatus == "Paid";
            bill.SecurityDeposit = firstBill && !depositPaidWithOrder
                ? PlanCatalog.DepositFor(connection.ConnectionType)
                : 0m;

            // 4) bulk discount from the number of connections of this customer
            int connectionCount = _context.Connections
                .Count(c => c.CustomerId == customerId && c.Status != "Permanently Inactive");
            decimal percent = BillingRules.BulkDiscountPercent(connectionCount);
            Discount? discount = null;
            if (percent > 0)
            {
                discount = _context.Discounts
                    .FirstOrDefault(d => d.IsActive && d.Type == "Percentage" && d.Value == percent);
                bill.DiscountId = discount?.DiscountId;
                // no matching slab row (e.g. deactivated): still apply the percentage
                discount ??= new Discount { Name = "Bulk discount", Type = "Percentage", Value = percent, IsActive = true };
            }

            // 5) discount, 12.24 % service tax and total
            BillingRules.Recalculate(bill, discount);

            // 6) carry-forward from the earlier bills of this connection
            var earlier = _context.Bills.Include(b => b.Payments)
                .Where(b => b.ConnectionId == connectionId)
                .ToList();
            bill.PreviousPayments = earlier.Sum(b => b.Payments.Sum(p => p.AmountPaid));
            bill.PreviousBalance = earlier.Sum(b => Math.Max(0m, b.TotalAmount - b.Payments.Sum(p => p.AmountPaid)));

            _context.Bills.Add(bill);
            _context.SaveChanges();

            return RedirectToAction("BillHistory");
        }

        //Add payments
        [HttpGet]
        public IActionResult AddPayment()
        {
            return View();
        }

        // Record a payment against a bill and update the bill status
        [HttpPost]
        public IActionResult AddPayment(int billId, decimal amountPaid, string paymentMethod, string referenceNo)
        {

            Bill? bill = _context.Bills.Find(billId);

            if (bill == null)
            {
                ViewBag.Error = "Bill not found";
                return View();
            }

            decimal alreadyPaid = _context.Payments.Where(p => p.BillId == billId).Sum(p => p.AmountPaid);
            if (amountPaid <= 0)
            {
                ViewBag.Error = "Enter an amount greater than zero.";
                return View();
            }
            if (amountPaid > bill.TotalAmount - alreadyPaid)
            {
                ViewBag.Error = "The amount is more than the balance of this bill ($" + (bill.TotalAmount - alreadyPaid).ToString("N2") + ").";
                return View();
            }


            //Payments

            Payment payment = new Payment();
            payment.BillId = billId;
            payment.PaymentDate = DateTime.Now;
            payment.AmountPaid = amountPaid;
            payment.PaymentMethod = paymentMethod;
            payment.ReferenceNo = referenceNo;

            _context.Payments.Add(payment);
            _context.SaveChanges();

            decimal totalPaid = _context.Payments.Where(p => p.BillId == billId).Sum(p => p.AmountPaid);

            if (totalPaid >= bill.TotalAmount)
            {
                bill.Status = "Paid";
            }
            else if (totalPaid > 0)
            {
                bill.Status = "Partial";
            }

            _context.SaveChanges();

            return RedirectToAction("Payments");
        }

      
        // First invoices of orders (plan + security deposit + tax) that are still unpaid
        public IActionResult OrderInvoices()
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.PaymentStatus == "Unpaid" && o.Status != "Rejected")
                .OrderBy(o => o.DueDate)
                .ToList();
            return View(orders);
        }

        // Mark the first invoice of an order as paid (customer paid at the counter or by transfer)
        [HttpPost]
        public IActionResult ReceiveOrderPayment(int id)
        {
            Order? order = _context.Orders.Find(id);
            if (order == null || order.PaymentStatus == "Paid" || order.Status == "Rejected")
            {
                TempData["Error"] = "This order invoice cannot be paid.";
                return RedirectToAction("OrderInvoices");
            }

            order.PaymentStatus = "Paid";
            order.PaidDate = DateTime.Now;
            _context.SaveChanges();

            TempData["Success"] = "Payment of $" + order.TotalAmount.ToString("N2") + " received for order " + order.OrderNumber + ".";
            return RedirectToAction("OrderInvoices");
        }

        // List of all payments
        public IActionResult Payments()
        {
            var payments = _context.Payments.ToList();
            return View(payments);
        }
        //Apply Discount

        // Show the discount form
        [HttpGet]
        public IActionResult ApplyDiscount()
        {
            ViewBag.Discounts = _context.Discounts.Where(d => d.IsActive).ToList();
            return View();
              
        }
        // Apply a discount to a bill and recalculate its total
        [HttpPost]
        public IActionResult ApplyDiscount(int billId, int discountId)
        {
            Bill? bill = _context.Bills.Find(billId);
            Discount? discount = _context.Discounts.Find(discountId);

                 if(bill == null || discount == null)
            {
                ViewBag.Error = "Bill or Discount not Found";
                ViewBag.Discounts = _context.Discounts.Where(d => d.IsActive).ToList();
                return View();
            }
            bill.DiscountId = discountId;
            BillingRules.Recalculate(bill, discount);

            _context.SaveChanges();
            return RedirectToAction("Dashboard");


        }
        //Due payments
        public IActionResult DuePayments()
        {
            var dueBills = _context.Bills
                .Where(b => b.Status != "Paid")
                .OrderBy(b => b.DueDate)
                .ToList();

            return View(dueBills);
        }

        //Bill History
        public IActionResult BillHistory()
        {
            var bills = _context.Bills
                .OrderByDescending(b => b.BillDate)
                .ToList();

            return View(bills);
        }
        //CalculateCharges 
        [HttpGet]
        public IActionResult CalculateCharges()
        {
            return View();
        }

        // Work out the plan charges of a connection and save them on the bill
        [HttpPost]
        public IActionResult CalculateCharges(int billId, string description, int quantity, decimal unitPrice)
        {
            Bill? bill = _context.Bills.Find(billId);

            if (bill == null)
            {
                ViewBag.Error = "Bill not found";
                return View();
            }

            BillingDetail detail = new BillingDetail();
            detail.BillId = billId;
            detail.Description = description;
            detail.Quantity = quantity;
            detail.UnitPrice = unitPrice;
            detail.Amount = quantity * unitPrice;

            _context.BillingDetails.Add(detail);
            _context.SaveChanges();

            bill.SubTotal = _context.BillingDetails.Where(d => d.BillId == billId).Sum(d => d.Amount);
            BillingRules.Recalculate(bill, LoadDiscount(bill));

            _context.SaveChanges();

            return RedirectToAction("BillHistory");
        }

        //SecurityDeposit
       [HttpGet]
        public IActionResult SecurityDeposit()
        {
            return View();
        }

        // Save the security deposit on a bill and recalculate its total
        [HttpPost]
        public IActionResult SecurityDeposit(int billId, decimal depositAmount)
        {
            Bill? bill = _context.Bills.Find(billId);

            if (bill == null)
            {
                ViewBag.Error = "Bill not found";
                return View();
            }

            bill.SecurityDeposit = depositAmount;
            BillingRules.Recalculate(bill, LoadDiscount(bill));

            _context.SaveChanges();

            return RedirectToAction("BillHistory");
        }

        //Connection

        // Connections list for picking the right connection id
        public IActionResult Connections()
        {
            var connections = _context.Connections
                .Include(c => c.Customer)
                .OrderBy(c => c.ConnectionNumber)
                .ToList();

            return View(connections);
        }

        // Customers list for picking the right customer id
        public IActionResult Customers()
        {
            var customers = _context.Customers
                .OrderBy(c => c.FullName)
                .ToList();

            return View(customers);
        }

        // the discount that is already stored on a bill (null when none)
        private Discount? LoadDiscount(Bill bill)
        {
            return bill.DiscountId.HasValue ? _context.Discounts.Find(bill.DiscountId.Value) : null;
        }
    }
}
