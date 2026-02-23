using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.QM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    [Authorize]
    public class QMController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public QMController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.CompanyID ?? 1;
        }

        // GET: QM/Inspections
        [HttpGet]
        public async Task<IActionResult> Inspections()
        {
            var companyId = await GetCompanyIdAsync();

            var inspections = await _context.QualityInspection
                .Include(q => q.ProductionBatch)
                    .ThenInclude(b => b.Product)
                .Include(q => q.User)
                .Where(q => q.CompanyID == companyId && q.Status == "ongoing")
                .OrderByDescending(q => q.QualityInspectionID)
                .Select(q => new QualityInspectionViewModel
                {
                    QualityInspectionID = q.QualityInspectionID,
                    ProductionBatchID = q.ProductionBatchID,
                    BatchCode = q.ProductionBatch != null ? (q.ProductionBatch.BatchCode ?? string.Empty) : string.Empty,
                    ProductName = q.ProductionBatch != null && q.ProductionBatch.Product != null 
                        ? (q.ProductionBatch.Product.ProductName ?? string.Empty) 
                        : string.Empty,
                    Result = q.Result ?? "N/A",
                    Status = q.Status ?? "ongoing",
                    UserName = q.User != null ? (q.User.UserName ?? q.User.Email ?? "Unknown") : "Unknown",
                    UserId = q.UserId,
                    Type = q.Type ?? string.Empty
                })
                .ToListAsync();

            var viewModel = new QualityInspectionPageViewModel
            {
                Inspections = inspections
            };

            return View(viewModel);
        }

        // GET: QM/Results
        [HttpGet]
        public async Task<IActionResult> Results()
        {
            var companyId = await GetCompanyIdAsync();

            var results = await _context.QualityInspection
                .Include(q => q.ProductionBatch)
                    .ThenInclude(b => b.Product)
                .Include(q => q.User)
                .Where(q => q.CompanyID == companyId && q.Status != "ongoing")
                .OrderByDescending(q => q.QualityInspectionID)
                .Select(q => new QualityInspectionResultViewModel
                {
                    QualityInspectionID = q.QualityInspectionID,
                    ProductionBatchID = q.ProductionBatchID,
                    ProductName = q.ProductionBatch != null && q.ProductionBatch.Product != null 
                        ? (q.ProductionBatch.Product.ProductName ?? string.Empty) 
                        : string.Empty,
                    Result = q.Result ?? "N/A",
                    Status = q.Status ?? string.Empty,
                    UserName = q.User != null ? (q.User.UserName ?? q.User.Email ?? "Unknown") : "Unknown",
                    Type = q.Type ?? string.Empty
                })
                .ToListAsync();

            var viewModel = new QualityInspectionResultsPageViewModel
            {
                Results = results
            };

            return View(viewModel);
        }

        // GET: QM/NonConformance
        [HttpGet]
        public IActionResult NonConformance()
        {
            return View();
        }

        // GET: QM/GetInspectionDetail/{id}
        [HttpGet]
        public async Task<IActionResult> GetInspectionDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();
            var inspection = await _context.QualityInspection
                .Include(q => q.ProductionBatch)
                    .ThenInclude(b => b.Product)
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.QualityInspectionID == id && q.CompanyID == companyId);

            if (inspection == null)
            {
                return NotFound();
            }

            var viewModel = new QualityInspectionDetailViewModel
            {
                QualityInspectionID = inspection.QualityInspectionID,
                ProductionBatchID = inspection.ProductionBatchID,
                BatchCode = inspection.ProductionBatch?.BatchCode ?? string.Empty,
                ProductName = inspection.ProductionBatch?.Product?.ProductName ?? string.Empty,
                Result = inspection.Result ?? "N/A",
                Status = inspection.Status ?? "ongoing",
                UserId = inspection.UserId,
                UserName = inspection.User?.UserName ?? inspection.User?.Email ?? "Unknown",
                Type = inspection.Type ?? string.Empty
            };

            return Json(viewModel);
        }

        // GET: QM/GetQualityCheckers
        [HttpGet]
        public async Task<IActionResult> GetQualityCheckers()
        {
            var companyId = await GetCompanyIdAsync();
            var qualityCheckerRole = await _roleManager.FindByNameAsync("QualityChecker");
            
            if (qualityCheckerRole == null)
            {
                return Json(new List<UserLookupViewModel>());
            }

            var qualityCheckers = await _userManager.GetUsersInRoleAsync("QualityChecker");
            
            var checkers = qualityCheckers
                .Where(u => u.CompanyID == companyId)
                .Select(u => new UserLookupViewModel
                {
                    Id = u.Id,
                    Name = u.UserName ?? u.Email ?? "Unknown"
                })
                .ToList();

            return Json(checkers);
        }

        // POST: QM/UpdateInspectionUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInspectionUser([FromBody] UpdateInspectionUserViewModel model)
        {
            var companyId = await GetCompanyIdAsync();
            var inspection = await _context.QualityInspection
                .FirstOrDefaultAsync(q => q.QualityInspectionID == model.InspectionID && q.CompanyID == companyId);

            if (inspection == null)
            {
                return NotFound();
            }

            inspection.UserId = model.UserId;
            _context.QualityInspection.Update(inspection);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Inspector assigned successfully." });
        }

        // POST: QM/PerformInspection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PerformInspection([FromBody] PerformInspectionViewModel model)
        {
            var companyId = await GetCompanyIdAsync();
            var inspection = await _context.QualityInspection
                .Include(q => q.ProductionBatch)
                    .ThenInclude(b => b.Product)
                .FirstOrDefaultAsync(q => q.QualityInspectionID == model.InspectionID && q.CompanyID == companyId);

            if (inspection == null)
            {
                return NotFound();
            }

            // Update inspection with test results
            inspection.Type = string.Join(", ", model.TestTypes);
            inspection.Result = model.Result;
            inspection.Status = "completed";

            // If passed, update product lifecycle status to "approved"
            if (model.Result == "pass" && inspection.ProductionBatch?.Product != null)
            {
                var product = inspection.ProductionBatch.Product;
                product.LifecycleStatus = "approved";
                _context.Products.Update(product);
            }

            _context.QualityInspection.Update(inspection);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Inspection completed successfully." });
        }
    }
}
