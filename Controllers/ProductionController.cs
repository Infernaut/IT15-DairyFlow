using IT15_DairyFlow.Data;
using IT15_DairyFlow.Hubs;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Production;
using IT15_DairyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Admin,ProductManager")]
    public class ProductionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<DairyFlowHub> _hub;
        private readonly NotificationService _notificationService;

        public ProductionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<DairyFlowHub> hub, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _hub = hub;
            _notificationService = notificationService;
        }

        // ─── BATCHES ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Batches()
        {
            var companyId = await GetCompanyIdAsync();
            await UpdateCompletedBatchesAsync(companyId);

            var productIdsWithFormulation = await _context.ProductFormulation
                .Where(f => f.CompanyID == companyId && f.IsActive)
                .Select(f => f.ProductID)
                .Distinct()
                .ToListAsync();

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
                    Quantity = b.Quantity,
                    ProductName = b.Product != null ? (b.Product.ProductName ?? string.Empty) : "Unknown",
                    EquipmentName = b.Equipment != null ? b.Equipment.EquipmentName : "Unknown",
                    UserName = b.User != null
                        ? (string.IsNullOrWhiteSpace(b.User.UserName) ? (b.User.Email ?? string.Empty) : b.User.UserName)
                        : "Unknown",
                    TotalCost = b.ProductionCosts.Any() ? b.ProductionCosts.Sum(c => c.TotalCost) : null
                })
                .ToListAsync();

            var products = await _context.Product
                .Where(p => p.CompanyID == companyId && p.LifecycleStatus == "Approved")
                .OrderBy(p => p.ProductName)
                .Select(p => new LookupItemViewModel
                {
                    Id = p.ProductID,
                    Name = p.ProductName ?? string.Empty,
                    HasFormulation = productIdsWithFormulation.Contains(p.ProductID)
                })
                .ToListAsync();

            var equipments = await _context.Equipment
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

        // ─── SCHEDULE ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            var companyId = await GetCompanyIdAsync();
            await UpdateCompletedBatchesAsync(companyId);

            var productIdsWithFormulation = await _context.ProductFormulation
                .Where(f => f.CompanyID == companyId && f.IsActive)
                .Select(f => f.ProductID)
                .Distinct()
                .ToListAsync();

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

            var products = await _context.Product
                .Where(p => p.CompanyID == companyId && p.LifecycleStatus == "Approved")
                .OrderBy(p => p.ProductName)
                .Select(p => new LookupItemViewModel
                {
                    Id = p.ProductID,
                    Name = p.ProductName ?? string.Empty,
                    HasFormulation = productIdsWithFormulation.Contains(p.ProductID)
                })
                .ToListAsync();

            var equipments = await _context.Equipment
                .Where(e => e.CompanyID == companyId && e.Status == "Available")
                .OrderBy(e => e.EquipmentName)
                .Select(e => new LookupItemViewModel
                {
                    Id = e.EquipmentID,
                    Name = e.EquipmentName
                })
                .ToListAsync();

            var viewModel = new ProductionSchedulePageViewModel
            {
                ScheduleItems = scheduleItems,
                Products = products,
                Equipments = equipments
            };

            return View(viewModel);
        }

        // ─── COSTS ────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Costs()
        {
            var companyId = await GetCompanyIdAsync();
            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var costs = await _context.ProductionCost
                .Include(pc => pc.ProductionBatch)
                    .ThenInclude(pb => pb.Product)
                .Where(pc => pc.CompanyID == companyId)
                .OrderByDescending(pc => pc.ProductionCostID)
                .Select(pc => new ProductionCostListViewModel
                {
                    ProductionCostID = pc.ProductionCostID,
                    BatchCode = pc.ProductionBatch != null ? (pc.ProductionBatch.BatchCode ?? string.Empty) : "Unknown",
                    ProductName = pc.ProductionBatch != null && pc.ProductionBatch.Product != null ? pc.ProductionBatch.Product.ProductName : "Unknown",
                    Quantity = pc.ProductionBatch != null ? pc.ProductionBatch.Quantity : 0,
                    TotalCost = pc.TotalCost,
                    StartDate = pc.ProductionBatch != null ? pc.ProductionBatch.StartDate : null,
                    EndDate = pc.ProductionBatch != null ? pc.ProductionBatch.EndDate : null
                })
                .ToListAsync();

            var viewModel = new ProductionCostPageViewModel
            {
                CostItems = costs,
                TotalCostAll = costs.Sum(c => c.TotalCost ?? 0),
                TotalCostToday = costs.Where(c => c.StartDate?.Date == today).Sum(c => c.TotalCost ?? 0),
                TotalCostWeek = costs.Where(c => c.StartDate?.Date >= startOfWeek).Sum(c => c.TotalCost ?? 0),
                TotalCostMonth = costs.Where(c => c.StartDate?.Date >= startOfMonth).Sum(c => c.TotalCost ?? 0),
                TotalCostYear = costs.Where(c => c.StartDate?.Date >= startOfYear).Sum(c => c.TotalCost ?? 0),
                TotalBatches = costs.Count,
                AverageCostPerBatch = costs.Count > 0 ? costs.Sum(c => c.TotalCost ?? 0) / costs.Count : 0
            };

            return View(viewModel);
        }

        // ─── HISTORY ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var companyId = await GetCompanyIdAsync();

            var history = await _context.ProductionBatch
                .Include(b => b.Product)
                .Include(b => b.Equipment)
                .Include(b => b.User)
                .Include(b => b.ProductionCosts)
                .Where(b => b.CompanyID == companyId && b.Status == "Completed")
                .OrderByDescending(b => b.EndDate)
                .Select(b => new ProductionHistoryItemViewModel
                {
                    ProductionBatchID = b.ProductionBatchID,
                    BatchCode = b.BatchCode ?? string.Empty,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Status = b.Status,
                    Quantity = b.Quantity,
                    ProductName = b.Product != null ? (b.Product.ProductName ?? string.Empty) : "Unknown",
                    EquipmentName = b.Equipment != null ? b.Equipment.EquipmentName : "Unknown",
                    UserName = b.User != null
                        ? (string.IsNullOrWhiteSpace(b.User.UserName) ? (b.User.Email ?? string.Empty) : b.User.UserName)
                        : "Unknown",
                    TotalCost = b.ProductionCosts.Any() ? b.ProductionCosts.Sum(c => c.TotalCost) : null
                })
                .ToListAsync();

            return View(history);
        }

        // ─── BATCH DETAIL ─────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetBatchDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var batch = await _context.ProductionBatch
                .Include(b => b.Product)
                .Include(b => b.Equipment)
                .Include(b => b.User)
                .Include(b => b.ProductionCosts)
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
                Quantity = batch.Quantity,
                TotalCost = batch.ProductionCosts.Any() ? batch.ProductionCosts.Sum(c => c.TotalCost) : null,
                ProductName = batch.Product != null ? (batch.Product.ProductName ?? string.Empty) : "Unknown",
                EquipmentName = batch.Equipment != null ? batch.Equipment.EquipmentName : "Unknown",
                UserName = batch.User != null
                    ? (string.IsNullOrWhiteSpace(batch.User.UserName) ? (batch.User.Email ?? string.Empty) : batch.User.UserName)
                    : "Unknown"
            };

            return Json(viewModel);
        }

        // ─── CHECK PRODUCT FORMULATION ────────────────────────────

        [HttpGet]
        public async Task<IActionResult> CheckProductFormulation(int productId)
        {
            var companyId = await GetCompanyIdAsync();

            var formulations = await _context.ProductFormulation
                .Include(f => f.RawMaterial)
                .Where(f => f.ProductID == productId && f.CompanyID == companyId && f.IsActive)
                .ToListAsync();

            bool hasFormulation = formulations.Any();
            decimal costPerUnit = formulations.Sum(f => f.Quantity * (f.RawMaterial?.UnitCost ?? 0));

            return Json(new { hasFormulation, costPerUnit });
        }

        // ─── CREATE BATCH ─────────────────────────────────────────

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

            var product = await _context.Product
                .FirstOrDefaultAsync(p => p.ProductID == model.ProductID && p.CompanyID == companyId);
            if (product == null)
            {
                return BadRequest("Invalid product selection.");
            }

            // Check if product has formulation (required for production)
            var hasFormulation = await _context.ProductFormulation
                .AnyAsync(f => f.ProductID == model.ProductID && f.CompanyID == companyId && f.IsActive);
            if (!hasFormulation)
            {
                return BadRequest("This product has no formulation. Please add a formulation before producing.");
            }

            var equipment = await _context.Equipment
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
                Status = model.Status,
                Quantity = model.Quantity
            };

            equipment.Status = "Busy";

            _context.ProductionBatch.Add(batch);
            _context.Equipment.Update(equipment);
            await _context.SaveChangesAsync();

            // Auto-calculate production cost from formulation
            var formulations = await _context.ProductFormulation
                .Include(f => f.RawMaterial)
                .Where(f => f.ProductID == model.ProductID && f.CompanyID == companyId && f.IsActive)
                .ToListAsync();

            decimal totalCost = formulations.Sum(f => f.Quantity * (f.RawMaterial?.UnitCost ?? 0)) * model.Quantity;

            var productionCost = new ProductionCost
            {
                ProductionBatchID = batch.ProductionBatchID,
                TotalCost = totalCost,
                CompanyID = companyId
            };
            _context.ProductionCost.Add(productionCost);
            await _context.SaveChangesAsync();

            // Auto-create quality inspection for completed batches
            if (model.Status == "Completed")
            {
                var qualityInspection = new QualityInspection
                {
                    CompanyID = companyId,
                    ProductionBatchID = batch.ProductionBatchID,
                    UserId = user.Id,
                    Type = string.Empty,
                    Result = "N/A",
                    Status = "ongoing"
                };

                _context.QualityInspection.Add(qualityInspection);
                await _context.SaveChangesAsync();
            }

            // SignalR: notify company group
            var productName = (await _context.Product.FindAsync(model.ProductID))?.ProductName ?? "";
            await _hub.NotifyBatchCreated(companyId, batch.BatchCode, productName, user.UserName ?? "");
            await _hub.NotifyDashboardRefresh(companyId, "Production");
            await _notificationService.NotifyCompanyActionAsync(
                user.Id, companyId, $"New batch {batch.BatchCode} ({productName}) created", "Production", "bi-box-seam");

            return Ok(new { success = true, message = "Production batch created successfully.", totalCost });
        }

        // ─── FINISH BATCH ─────────────────────────────────────────

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
            
            if (batch.Equipment != null)
            {
                batch.Equipment.Status = "Available";
            }

            await _context.SaveChangesAsync();
            
            // Auto-create quality inspection for ALL completed batches
            // Products go to finished goods only after QM pass/release
            {
                var user = await _userManager.GetUserAsync(User);
                var qualityInspection = new QualityInspection
                {
                    CompanyID = companyId,
                    ProductionBatchID = batch.ProductionBatchID,
                    UserId = user?.Id ?? string.Empty,
                    Type = string.Empty,
                    Result = "N/A",
                    Status = "ongoing"
                };

                _context.QualityInspection.Add(qualityInspection);
                await _context.SaveChangesAsync();
            }
            
            // SignalR: notify company group
            await _hub.NotifyBatchCompleted(companyId, batch.BatchCode, batch.Product?.ProductName ?? "");
            await _hub.NotifyDashboardRefresh(companyId, "Production");
            var finishUser = await _userManager.GetUserAsync(User);
            await _notificationService.NotifyCompanyActionAsync(
                finishUser?.Id ?? "", companyId, $"Batch {batch.BatchCode} completed", "Production", "bi-check-circle");

            return Ok(new { success = true, message = "Batch finished successfully." });
        }

        // ─── SUPPLIERS ────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Suppliers()
        {
            var companyId = await GetCompanyIdAsync();

            var suppliers = await _context.Supplier
                .Where(s => s.CompanyID == companyId)
                .OrderBy(s => s.SupplierName)
                .Select(s => new SupplierListViewModel
                {
                    SupplierID = s.SupplierID,
                    SupplierName = s.SupplierName ?? string.Empty,
                    ContactInfo = s.ContactInfo,
                    Status = s.Status
                })
                .ToListAsync();

            var viewModel = new SupplierPageViewModel
            {
                Suppliers = suppliers
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var supplier = await _context.Supplier
                .FirstOrDefaultAsync(s => s.SupplierID == id && s.CompanyID == companyId);

            if (supplier == null)
            {
                return NotFound();
            }

            return Json(new
            {
                supplierID = supplier.SupplierID,
                supplierName = supplier.SupplierName,
                contactInfo = supplier.ContactInfo,
                status = supplier.Status
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var supplier = new Supplier
            {
                CompanyID = companyId,
                SupplierName = model.SupplierName,
                ContactInfo = model.ContactInfo,
                Status = "Active"
            };

            _context.Supplier.Add(supplier);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Supplier created successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSupplier([FromBody] UpdateSupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var supplier = await _context.Supplier
                .FirstOrDefaultAsync(s => s.SupplierID == model.SupplierID && s.CompanyID == companyId);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.SupplierName = model.SupplierName;
            supplier.ContactInfo = model.ContactInfo;
            _context.Supplier.Update(supplier);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Supplier updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSupplierStatus(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var supplier = await _context.Supplier
                .FirstOrDefaultAsync(s => s.SupplierID == id && s.CompanyID == companyId);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.Status = supplier.Status == "Active" ? "Inactive" : "Active";
            _context.Supplier.Update(supplier);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Supplier set to {supplier.Status}." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var supplier = await _context.Supplier
                .FirstOrDefaultAsync(s => s.SupplierID == id && s.CompanyID == companyId);

            if (supplier == null)
            {
                return NotFound();
            }

            // Check if supplier has raw materials
            var hasRawMaterials = await _context.RawMaterial
                .AnyAsync(r => r.SupplierID == id);

            if (hasRawMaterials)
            {
                return BadRequest("Cannot delete supplier with associated raw materials. Set to Inactive instead.");
            }

            _context.Supplier.Remove(supplier);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Supplier deleted successfully." });
        }

        // ─── HELPERS ──────────────────────────────────────────────

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

            var user = await _userManager.GetUserAsync(User);

            foreach (var batch in batches)
            {
                batch.Status = "Completed";
                
                if (batch.Equipment != null)
                {
                    batch.Equipment.Status = "Available";
                }
                
                // Auto-create quality inspection for ALL completed batches
                // Products go to finished goods only after QM pass/release
                var qualityInspection = new QualityInspection
                {
                    CompanyID = companyId,
                    ProductionBatchID = batch.ProductionBatchID,
                    UserId = user?.Id ?? string.Empty,
                    Type = string.Empty,
                    Result = "N/A",
                    Status = "ongoing"
                };

                _context.QualityInspection.Add(qualityInspection);
            }

            await _context.SaveChangesAsync();
        }

        private static string GenerateBatchCode()
        {
            return $"BATCH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }

        private async Task AddToInventoryAsync(int companyId, int productId, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            var existing = await _context.Inventory
                .FirstOrDefaultAsync(i => i.ProductID == productId && i.CompanyID == companyId);

            if (existing != null)
            {
                existing.Quantity = (existing.Quantity ?? 0) + quantity;
            }
            else
            {
                _context.Inventory.Add(new Inventory
                {
                    ProductID = productId,
                    CompanyID = companyId,
                    UserID = user?.Id ?? string.Empty,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
