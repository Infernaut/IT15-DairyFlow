using System.Diagnostics;
using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel();

            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Superadmin"))
            {
                model.TotalCompanies = 0;
                model.ActiveSubscriptions = 0;
                model.TrialCompanies = 0;
                model.SystemInstances = 0;
                model.MonthlyRecurringRevenue = 0m;
            }
            else if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                var now = DateTimeOffset.UtcNow;
                model.TotalUsers = await _userManager.Users.CountAsync();
                model.ActiveUsers = await _userManager.Users.CountAsync(user =>
                    !user.LockoutEnd.HasValue || user.LockoutEnd <= now);
                model.InactiveUsers = await _userManager.Users.CountAsync(user =>
                    user.LockoutEnd.HasValue && user.LockoutEnd > now);
                model.UsersWithRoles = await _dbContext.UserRoles
                    .Select(userRole => userRole.UserId)
                    .Distinct()
                    .CountAsync();

                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                model.AdminUsers = admins.Count;
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
