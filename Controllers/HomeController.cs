using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunicationSystem.Data;
using NexusCommunicationSystem.Models;

namespace NexusCommunicationSystem.Controllers
{
    // Portal gateway: one landing page that links to the four logins.
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Landing page with the portal links and the real active plans
        public async Task<IActionResult> Index()
        {
            var plans = await _db.Plans
                .Where(p => p.IsActive)
                .OrderBy(p => p.ConnectionType).ThenBy(p => p.Price)
                .ToListAsync();
            return View(plans);
        }

        // Generic error page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
