using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    [Authorize]
    public class ProductionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Batches()
        {
            var companyId = await GetCompanyIdAsync();
            await UpdateCompletedBatchesAsync(companyId);

            var batches = await _context.ProductionBatch
                .Include(b => b.Product)
                .Include(b => b.Equipment)
                .Include(b => b.User)
                .Where(b => b.CompanyID == companyId && b.Status != "Completed")
                .OrderByDescending(b => b.StartDate)
                .Select(b => new ProductionBatchListViewModel
                {
                    ProductionBatchID = b.ProductionBatchID,
                    BatchCode = b.BatchCode ?? string.Empty,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Status = b.Status,
                    ProductName = b.Product != null ? (b.Product.ProductName ?? string.Empty) : "Unknown",
                    EquipmentName = b.Equipment != null ? b.Equipment.EquipmentName : "Unknown",
                    UserName = b.User != null
                        ? (string.IsNullOrWhiteSpace(b.User.UserName) ? (b.User.Email ?? string.Empty) : b.User.UserName)
                        : "Unknown"
                })
                .ToListAsync();

            var products = await _context.Products
                .Where(p => p.CompanyID == companyId && p.LifecycleStatus != "Archived")
                .OrderBy(p => p.ProductName)
                .Select(p => new LookupItemViewModel
                {
                    Id = p.ProductID,
                    Name = p.ProductName ?? string.Empty
                })
                .ToListAsync();

            var equipments = await _context.Equipments
                .Where(e => e.CompanyID == companyId && e.Status == "Available")
                .OrderBy(e => e.EquipmentName)
                .Select(e => new LookupItemViewModel
                {
                    Id = e.EquipmentID,
                    Name = e.EquipmentName
                })
                .ToListAsync();

            var viewModel = new ProductionBatchPageViewModel
            {
                Batches = batches,
                Products = products,
                Equipments = equipments
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            var companyId = await GetCompanyIdAsync();
            await UpdateCompletedBatchesAsync(companyId);

            var scheduleItems = await _context.ProductionBatch
                .Include(b => b.Product)
                .Include(b => b.Equipment)
                .Where(b => b.CompanyID == companyId)
                .OrderBy(b => b.StartDate)
                .Select(b => new ProductionScheduleItemViewModel
                {
                    ProductionBatchID = b.ProductionBatchID,
                    BatchCode = b.BatchCode ?? string.Empty,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Status = b.Status,
                    ProductName = b.Product != null ? (b.Product.ProductName ?? string.Empty) : "Unknown",
                    EquipmentName = b.Equipment != null ? b.Equipment.EquipmentName : "Unknown"
                })
                .ToListAsync();

            return View(scheduleItems);
        }

        [HttpGet]
        public async Task<IActionResult> Costs()
        {
            var companyId = await GetCompanyIdAsync();

            var costs = await _context.ProductionCosts
                .Include(pc => pc.ProductionBatch)
                .Where(pc => pc.CompanyID == companyId)
                .OrderByDescending(pc => pc.ProductionCostID)
                .Select(pc => new ProductionCostListViewModel
                {
                    ProductionCostID = pc.ProductionCostID,
                    BatchCode = pc.ProductionBatch != null ? (pc.ProductionBatch.BatchCode ?? string.Empty) : "Unknown",
                    TotalCost = pc.TotalCost,
                    StartDate = pc.ProductionBatch != null ? pc.ProductionBatch.StartDate : null,
                    EndDate = pc.ProductionBatch != null ? pc.ProductionBatch.EndDate : null
                })
                .ToListAsync();

            return View(costs);
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var batch = await _context.ProductionBatch
                .Include(b => b.Product)
                .Include(b => b.Equipment)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.ProductionBatchID == id && b.CompanyID == companyId);

            if (batch == null)
            {
                return NotFound();
            }

            var viewModel = new ProductionBatchDetailViewModel
            {
                ProductionBatchID = batch.ProductionBatchID,
                BatchCode = batch.BatchCode ?? string.Empty,
                StartDate = batch.StartDate,
                EndDate = batch.EndDate,
                Status = batch.Status,
                ProductName = batch.Product != null ? (batch.Product.ProductName ?? string.Empty) : "Unknown",
                EquipmentName = batch.Equipment != null ? batch.Equipment.EquipmentName : "Unknown",
                UserName = batch.User != null
                    ? (string.IsNullOrWhiteSpace(batch.User.UserName) ? (batch.User.Email ?? string.Empty) : batch.User.UserName)
                    : "Unknown"
            };

            return Json(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBatch([FromBody] CreateProductionBatchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var companyId = user.CompanyID ?? 1;

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductID == model.ProductID && p.CompanyID == companyId);
            if (product == null)
            {
                return BadRequest("Invalid product selection.");
            }

            var equipment = await _context.Equipments
                .FirstOrDefaultAsync(e => e.EquipmentID == model.EquipmentID && e.CompanyID == companyId && e.Status == "Available");
            if (equipment == null)
            {
                return BadRequest("Selected equipment is not available.");
            }

            var batch = new ProductionBatch
            {
                CompanyID = companyId,
                ProductID = model.ProductID,
                EquipmentID = model.EquipmentID,
                UserID = user.Id,
                BatchCode = GenerateBatchCode(),
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Status = model.Status
            };

            product.LifecycleStatus = "In Production";
            equipment.Status = "Busy";

            _context.ProductionBatch.Add(batch);
            _context.Products.Update(product);
            _context.Equipments.Update(equipment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Production batch created successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinishBatch(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var batch = await _context.ProductionBatch
                .Include(b => b.Product)
                .Include(b => b.Equipment)
                .FirstOrDefaultAsync(b => b.ProductionBatchID == id && b.CompanyID == companyId);

            if (batch == null)
            {
                return NotFound();
            }

            batch.Status = "Completed";
            batch.EndDate = batch.EndDate ?? DateTime.UtcNow;
            if (batch.Product != null)
            {
                batch.Product.LifecycleStatus = "Produced";
            }
            if (batch.Equipment != null)
            {
                batch.Equipment.Status = "Available";
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Batch finished successfully." });
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.CompanyID ?? 1;
        }

        private async Task UpdateCompletedBatchesAsync(int companyId)
        {
            var now = DateTime.UtcNow;
            var batches = await _context.ProductionBatch
                .Include(b => b.Product)
                .Include(b => b.Equipment)
                .Where(b => b.CompanyID == companyId && b.EndDate.HasValue && b.EndDate <= now && b.Status != "Completed")
                .ToListAsync();

            if (batches.Count == 0)
            {
                return;
            }

            foreach (var batch in batches)
            {
                batch.Status = "Completed";
                if (batch.Product != null)
                {
                    batch.Product.LifecycleStatus = "Produced";
                }
                if (batch.Equipment != null)
                {
                    batch.Equipment.Status = "Available";
                }
            }

            await _context.SaveChangesAsync();
        }

        private static string GenerateBatchCode()
        {
            return $"BATCH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }
    }
}
