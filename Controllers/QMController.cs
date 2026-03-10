using IT15_DairyFlow.Data;
using IT15_DairyFlow.Hubs;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.QM;
using IT15_DairyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Admin,QualityChecker,ProductManager")]
    public class QMController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHubContext<DairyFlowHub> _hub;
        private readonly NotificationService _notificationService;

        public QMController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IHubContext<DairyFlowHub> hub, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _hub = hub;
            _notificationService = notificationService;
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
            var today = DateTime.UtcNow.Date;

            var inspections = await _context.QualityInspection
                .Include(q => q.ProductionBatch)
                    .ThenInclude(b => b.Product)
                .Include(q => q.User)
                .Where(q => q.CompanyID == companyId && (q.Status == "ongoing" || q.Status == "on-hold"))
                .OrderByDescending(q => q.QualityInspectionID)
                .Select(q => new QualityInspectionViewModel
                {
                    QualityInspectionID = q.QualityInspectionID,
                    ProductionBatchID = q.ProductionBatchID,
                    BatchCode = q.ProductionBatch != null ? (q.ProductionBatch.BatchCode ?? string.Empty) : string.Empty,
                    ProductName = q.ProductionBatch != null && q.ProductionBatch.Product != null 
                        ? (q.ProductionBatch.Product.ProductName ?? string.Empty) 
                        : string.Empty,
                    InspectionType = q.InspectionType ?? "FinalProduct",
                    Result = q.Result ?? "Pending",
                    Status = q.Status ?? "ongoing",
                    UserName = q.User != null ? (q.User.UserName ?? q.User.Email ?? "Unknown") : "Unknown",
                    UserId = q.UserId,
                    Type = q.Type ?? string.Empty,
                    InspectionDate = q.InspectionDate
                })
                .ToListAsync();

            // Get summary stats
            var allInspections = await _context.QualityInspection
                .Where(q => q.CompanyID == companyId)
                .ToListAsync();

            var summary = new QualityInspectionSummaryViewModel
            {
                TotalPending = allInspections.Count(q => q.Status == "ongoing"),
                TotalOnHold = allInspections.Count(q => q.Status == "on-hold"),
                PassedToday = allInspections.Count(q => q.Result == "pass" && q.CompletedDate?.Date == today),
                FailedToday = allInspections.Count(q => q.Result == "fail" && q.CompletedDate?.Date == today),
                OpenNCRs = 0 // Non-conformance reports commented out
                // await _context.NonConformance.CountAsync(nc => nc.CompanyID == companyId && nc.Status != "Closed")
            };

            // Get quality checkers
            var qualityCheckers = await GetQualityCheckersListAsync(companyId);

            var viewModel = new QualityInspectionPageViewModel
            {
                Inspections = inspections,
                Summary = summary,
                QualityCheckers = qualityCheckers
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
                .Where(q => q.CompanyID == companyId && (q.Status == "completed" || q.Status == "released" || q.Status == "rejected"))
                .OrderByDescending(q => q.CompletedDate ?? q.InspectionDate)
                .Select(q => new QualityInspectionResultViewModel
                {
                    QualityInspectionID = q.QualityInspectionID,
                    ProductionBatchID = q.ProductionBatchID,
                    BatchCode = q.ProductionBatch != null ? (q.ProductionBatch.BatchCode ?? string.Empty) : string.Empty,
                    ProductName = q.ProductionBatch != null && q.ProductionBatch.Product != null 
                        ? (q.ProductionBatch.Product.ProductName ?? string.Empty) 
                        : string.Empty,
                    InspectionType = q.InspectionType ?? "FinalProduct",
                    Result = q.Result ?? "N/A",
                    Status = q.Status ?? string.Empty,
                    UserName = q.User != null ? (q.User.UserName ?? q.User.Email ?? "Unknown") : "Unknown",
                    Type = q.Type ?? string.Empty,
                    InspectionDate = q.InspectionDate,
                    CompletedDate = q.CompletedDate
                })
                .ToListAsync();

            var viewModel = new QualityInspectionResultsPageViewModel
            {
                Results = results
            };

            return View(viewModel);
        }

        // GET: QM/NonConformance - COMMENTED OUT
        /*
        [HttpGet]
        public async Task<IActionResult> NonConformance()
        {
            var companyId = await GetCompanyIdAsync();
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var ncrs = await _context.NonConformance
                .Include(nc => nc.ReportedByUser)
                .Include(nc => nc.AssignedToUser)
                .Include(nc => nc.ProductionBatch)
                .Where(nc => nc.CompanyID == companyId)
                .OrderByDescending(nc => nc.ReportedDate)
                .Select(nc => new NonConformanceViewModel
                {
                    NonConformanceID = nc.NonConformanceID,
                    NCRNumber = nc.NCRNumber,
                    Category = nc.Category,
                    Severity = nc.Severity,
                    Title = nc.Title,
                    Status = nc.Status,
                    ReportedBy = nc.ReportedByUser.UserName ?? nc.ReportedByUser.Email ?? "Unknown",
                    AssignedTo = nc.AssignedToUser != null ? (nc.AssignedToUser.UserName ?? nc.AssignedToUser.Email) : null,
                    ReportedDate = nc.ReportedDate,
                    DueDate = nc.DueDate,
                    BatchCode = nc.ProductionBatch != null ? nc.ProductionBatch.BatchCode : null
                })
                .ToListAsync();

            var allNcrs = await _context.NonConformance
                .Where(nc => nc.CompanyID == companyId)
                .ToListAsync();

            var summary = new NonConformanceSummaryViewModel
            {
                TotalOpen = allNcrs.Count(nc => nc.Status == "Open"),
                TotalInProgress = allNcrs.Count(nc => nc.Status == "InProgress"),
                PendingVerification = allNcrs.Count(nc => nc.Status == "PendingVerification"),
                ClosedThisMonth = allNcrs.Count(nc => nc.Status == "Closed" && nc.ClosedDate >= startOfMonth),
                OverdueCount = allNcrs.Count(nc => nc.Status != "Closed" && nc.DueDate < today),
                TotalCostImpact = allNcrs.Where(nc => nc.CostImpact.HasValue).Sum(nc => nc.CostImpact!.Value)
            };

            var users = await GetCompanyUsersAsync(companyId);

            var viewModel = new NonConformancePageViewModel
            {
                NonConformances = ncrs,
                Summary = summary,
                Users = users
            };

            return View(viewModel);
        }
        */

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
                InspectionType = inspection.InspectionType ?? "FinalProduct",
                Result = inspection.Result ?? "Pending",
                Status = inspection.Status ?? "ongoing",
                UserId = inspection.UserId,
                UserName = inspection.User?.UserName ?? inspection.User?.Email ?? "Unknown",
                Type = inspection.Type ?? string.Empty,
                SampleLot = inspection.SampleLot,
                SampleSize = inspection.SampleSize,
                InspectionDate = inspection.InspectionDate,
                CompletedDate = inspection.CompletedDate,
                Notes = inspection.Notes,
                Temperature = inspection.Temperature,
                PHLevel = inspection.PHLevel,
                FatContent = inspection.FatContent,
                ProteinContent = inspection.ProteinContent,
                MoistureContent = inspection.MoistureContent,
                Acidity = inspection.Acidity,
                BacterialCount = inspection.BacterialCount,
                SomaticCellCount = inspection.SomaticCellCount,
                AntibioticTest = inspection.AntibioticTest
            };

            return Json(viewModel);
        }

        // GET: QM/GetQualityCheckers
        [HttpGet]
        public async Task<IActionResult> GetQualityCheckers()
        {
            var companyId = await GetCompanyIdAsync();
            var checkers = await GetQualityCheckersListAsync(companyId);
            return Json(checkers);
        }

        private async Task<List<UserLookupViewModel>> GetQualityCheckersListAsync(int companyId)
        {
            var qualityCheckerRole = await _roleManager.FindByNameAsync("QualityChecker");
            
            if (qualityCheckerRole == null)
            {
                return new List<UserLookupViewModel>();
            }

            var qualityCheckers = await _userManager.GetUsersInRoleAsync("QualityChecker");
            
            return qualityCheckers
                .Where(u => u.CompanyID == companyId)
                .Select(u => new UserLookupViewModel
                {
                    Id = u.Id,
                    Name = u.UserName ?? u.Email ?? "Unknown"
                })
                .ToList();
        }

        private async Task<List<UserLookupViewModel>> GetCompanyUsersAsync(int companyId)
        {
            return await _context.Users
                .Where(u => u.CompanyID == companyId)
                .Select(u => new UserLookupViewModel
                {
                    Id = u.Id,
                    Name = u.UserName ?? u.Email ?? "Unknown"
                })
                .ToListAsync();
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
        [Authorize(Roles = "Admin,QualityChecker")]
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
            inspection.Type = model.TestTypes.Any() ? string.Join(", ", model.TestTypes) : inspection.Type;
            inspection.Result = model.Result;
            inspection.SampleLot = model.SampleLot;
            inspection.SampleSize = model.SampleSize;
            inspection.Notes = model.Notes;
            inspection.InspectionDate = inspection.InspectionDate ?? DateTime.UtcNow;
            inspection.CompletedDate = DateTime.UtcNow;

            // Update test parameters
            inspection.Temperature = model.Temperature;
            inspection.PHLevel = model.PHLevel;
            inspection.FatContent = model.FatContent;
            inspection.ProteinContent = model.ProteinContent;
            inspection.MoistureContent = model.MoistureContent;
            inspection.Acidity = model.Acidity;
            inspection.BacterialCount = model.BacterialCount;
            inspection.SomaticCellCount = model.SomaticCellCount;
            inspection.AntibioticTest = model.AntibioticTest;

            // Determine status based on result
            if (model.Result == "pass")
            {
                inspection.Status = "released";
                // Update product lifecycle status to "approved"
                if (inspection.ProductionBatch?.Product != null)
                {
                    var product = inspection.ProductionBatch.Product;
                    product.LifecycleStatus = "approved";
                    _context.Product.Update(product);
                }
                // Auto-add to finished goods inventory after QM pass
                if (inspection.ProductionBatch != null)
                {
                    await AddToInventoryAsync(companyId, inspection.ProductionBatch.ProductID, inspection.ProductionBatch.Quantity);
                }
            }
            else if (model.Result == "fail")
            {
                inspection.Status = "rejected";
            }
            else
            {
                inspection.Status = "completed";
            }

            _context.QualityInspection.Update(inspection);
            await _context.SaveChangesAsync();

            // SignalR: notify inspection completed
            var user = await _userManager.GetUserAsync(User);
            var batchCode = inspection.ProductionBatch?.BatchCode ?? "";
            await _hub.NotifyInspectionCompleted(companyId, batchCode, model.Result, user?.UserName ?? "");
            await _hub.NotifyDashboardRefresh(companyId, "Quality");
            await _notificationService.NotifyCompanyActionAsync(
                user?.Id ?? "", companyId, $"Inspection {model.Result} for batch {batchCode}", "Quality", model.Result == "pass" ? "bi-check-circle" : "bi-x-circle");

            return Ok(new { success = true, message = "Inspection completed successfully." });
        }

        // POST: QM/HoldBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,QualityChecker")]
        public async Task<IActionResult> HoldBatch([FromBody] HoldBatchViewModel model)
        {
            var companyId = await GetCompanyIdAsync();
            var inspection = await _context.QualityInspection
                .FirstOrDefaultAsync(q => q.QualityInspectionID == model.InspectionID && q.CompanyID == companyId);

            if (inspection == null)
            {
                return NotFound();
            }

            inspection.Status = "on-hold";
            inspection.Notes = model.Reason;
            _context.QualityInspection.Update(inspection);
            await _context.SaveChangesAsync();

            // SignalR: notify batch on hold
            await _hub.NotifyBatchOnHold(companyId, $"Inspection #{model.InspectionID}", model.Reason ?? "");
            await _hub.NotifyDashboardRefresh(companyId, "Quality");
            var holdUser = await _userManager.GetUserAsync(User);
            await _notificationService.NotifyCompanyActionAsync(
                holdUser?.Id ?? "", companyId, $"Batch placed on hold: Inspection #{model.InspectionID}", "Quality", "bi-pause-circle");

            return Ok(new { success = true, message = "Batch placed on hold." });
        }

        // POST: QM/ReleaseBatch
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,QualityChecker")]
        public async Task<IActionResult> ReleaseBatch([FromBody] ReleaseBatchViewModel model)
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

            inspection.Status = "released";
            inspection.Result = "pass";
            inspection.CompletedDate = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(model.Notes))
            {
                inspection.Notes = (inspection.Notes ?? "") + "\n" + model.Notes;
            }

            // Update product lifecycle
            if (inspection.ProductionBatch?.Product != null)
            {
                inspection.ProductionBatch.Product.LifecycleStatus = "approved";
                _context.Product.Update(inspection.ProductionBatch.Product);
            }

            // Auto-add to finished goods inventory after QM release
            if (inspection.ProductionBatch != null)
            {
                await AddToInventoryAsync(companyId, inspection.ProductionBatch.ProductID, inspection.ProductionBatch.Quantity);
            }

            _context.QualityInspection.Update(inspection);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Batch released successfully." });
        }

        // ==================== NonConformance Endpoints - COMMENTED OUT ====================
        /*
        // GET: QM/GetNonConformanceDetail/{id}
        [HttpGet]
        public async Task<IActionResult> GetNonConformanceDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();
            var ncr = await _context.NonConformance
                .Include(nc => nc.ReportedByUser)
                .Include(nc => nc.AssignedToUser)
                .Include(nc => nc.ProductionBatch)
                .FirstOrDefaultAsync(nc => nc.NonConformanceID == id && nc.CompanyID == companyId);

            if (ncr == null)
            {
                return NotFound();
            }

            var viewModel = new NonConformanceDetailViewModel
            {
                NonConformanceID = ncr.NonConformanceID,
                NCRNumber = ncr.NCRNumber,
                QualityInspectionID = ncr.QualityInspectionID,
                ProductionBatchID = ncr.ProductionBatchID,
                BatchCode = ncr.ProductionBatch?.BatchCode,
                Category = ncr.Category,
                Severity = ncr.Severity,
                Title = ncr.Title,
                Description = ncr.Description,
                RootCause = ncr.RootCause,
                ImmediateAction = ncr.ImmediateAction,
                CorrectiveAction = ncr.CorrectiveAction,
                PreventiveAction = ncr.PreventiveAction,
                Status = ncr.Status,
                AffectedQuantity = ncr.AffectedQuantity,
                CostImpact = ncr.CostImpact,
                Disposition = ncr.Disposition,
                ReportedDate = ncr.ReportedDate,
                DueDate = ncr.DueDate,
                ClosedDate = ncr.ClosedDate,
                ReportedById = ncr.ReportedByUserId,
                ReportedByName = ncr.ReportedByUser?.UserName ?? ncr.ReportedByUser?.Email ?? "Unknown",
                AssignedToId = ncr.AssignedToUserId,
                AssignedToName = ncr.AssignedToUser?.UserName ?? ncr.AssignedToUser?.Email
            };

            return Json(viewModel);
        }

        // POST: QM/CreateNonConformance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNonConformance([FromBody] CreateNonConformanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();
            var user = await _userManager.GetUserAsync(User);

            // Generate NCR number
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
            var count = await _context.NonConformance
                .CountAsync(nc => nc.CompanyID == companyId && nc.NCRNumber.StartsWith($"NCR-{yearMonth}"));
            var ncrNumber = $"NCR-{yearMonth}-{(count + 1):D4}";

            var ncr = new NonConformance
            {
                CompanyID = companyId,
                NCRNumber = ncrNumber,
                QualityInspectionID = model.QualityInspectionID,
                ProductionBatchID = model.ProductionBatchID,
                ReportedByUserId = user?.Id ?? string.Empty,
                AssignedToUserId = model.AssignedToUserId,
                Category = model.Category,
                Severity = model.Severity,
                Title = model.Title,
                Description = model.Description,
                ImmediateAction = model.ImmediateAction,
                AffectedQuantity = model.AffectedQuantity,
                DueDate = model.DueDate,
                Status = "Open",
                ReportedDate = DateTime.UtcNow
            };

            _context.NonConformance.Add(ncr);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Non-conformance report created.", ncrNumber = ncrNumber });
        }

        // POST: QM/UpdateNonConformance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNonConformance([FromBody] UpdateNonConformanceViewModel model)
        {
            var companyId = await GetCompanyIdAsync();
            var ncr = await _context.NonConformance
                .FirstOrDefaultAsync(nc => nc.NonConformanceID == model.NonConformanceID && nc.CompanyID == companyId);

            if (ncr == null)
            {
                return NotFound();
            }

            ncr.RootCause = model.RootCause ?? ncr.RootCause;
            ncr.CorrectiveAction = model.CorrectiveAction ?? ncr.CorrectiveAction;
            ncr.PreventiveAction = model.PreventiveAction ?? ncr.PreventiveAction;
            ncr.Disposition = model.Disposition ?? ncr.Disposition;
            ncr.CostImpact = model.CostImpact ?? ncr.CostImpact;
            ncr.AssignedToUserId = model.AssignedToUserId ?? ncr.AssignedToUserId;
            ncr.DueDate = model.DueDate ?? ncr.DueDate;

            if (!string.IsNullOrEmpty(model.Status))
            {
                ncr.Status = model.Status;
                if (model.Status == "InProgress" && ncr.Status == "Open")
                {
                    ncr.Status = "InProgress";
                }
                else if (model.Status == "Closed")
                {
                    ncr.Status = "Closed";
                    ncr.ClosedDate = DateTime.UtcNow;
                }
            }

            _context.NonConformance.Update(ncr);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Non-conformance updated successfully." });
        }

        // POST: QM/CloseNonConformance/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseNonConformance(int id)
        {
            var companyId = await GetCompanyIdAsync();
            var user = await _userManager.GetUserAsync(User);

            var ncr = await _context.NonConformance
                .FirstOrDefaultAsync(nc => nc.NonConformanceID == id && nc.CompanyID == companyId);

            if (ncr == null)
            {
                return NotFound();
            }

            ncr.Status = "Closed";
            ncr.ClosedDate = DateTime.UtcNow;
            ncr.VerifiedByUserId = user?.Id;
            ncr.VerifiedDate = DateTime.UtcNow;

            _context.NonConformance.Update(ncr);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Non-conformance closed successfully." });
        }

        // POST: QM/CreateNCRFromInspection/{inspectionId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNCRFromInspection(int inspectionId)
        {
            var companyId = await GetCompanyIdAsync();
            var user = await _userManager.GetUserAsync(User);

            var inspection = await _context.QualityInspection
                .Include(q => q.ProductionBatch)
                .FirstOrDefaultAsync(q => q.QualityInspectionID == inspectionId && q.CompanyID == companyId);

            if (inspection == null)
            {
                return NotFound();
            }

            // Generate NCR number
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
            var count = await _context.NonConformance
                .CountAsync(nc => nc.CompanyID == companyId && nc.NCRNumber.StartsWith($"NCR-{yearMonth}"));
            var ncrNumber = $"NCR-{yearMonth}-{(count + 1):D4}";

            var ncr = new NonConformance
            {
                CompanyID = companyId,
                NCRNumber = ncrNumber,
                QualityInspectionID = inspectionId,
                ProductionBatchID = inspection.ProductionBatchID,
                ReportedByUserId = user?.Id ?? string.Empty,
                Category = "Product",
                Severity = "Major",
                Title = $"QC Failure - Batch {inspection.ProductionBatch?.BatchCode ?? "Unknown"}",
                Description = $"Quality inspection failed. Tests performed: {inspection.Type ?? "N/A"}. Notes: {inspection.Notes ?? "N/A"}",
                Status = "Open",
                ReportedDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            _context.NonConformance.Add(ncr);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "NCR created from inspection.", ncrNumber = ncrNumber });
        }

        // GET: QM/GetBatches - Get batches for NCR creation
        [HttpGet]
        public async Task<IActionResult> GetBatches()
        {
            var companyId = await GetCompanyIdAsync();
            var batches = await _context.ProductionBatch
                .Where(b => b.CompanyID == companyId)
                .OrderByDescending(b => b.ProductionBatchID)
                .Take(50)
                .Select(b => new
                {
                    id = b.ProductionBatchID,
                    code = b.BatchCode ?? $"Batch-{b.ProductionBatchID}"
                })
                .ToListAsync();

            return Json(batches);
        }
        */   // End of commented-out NonConformance section

        private async Task AddToInventoryAsync(int companyId, int productId, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);

            // Get product's shelf life for expiry date calculation
            var product = await _context.Product.FindAsync(productId);
            DateTime? expiryDate = null;
            if (product?.ShelfLifeDays != null && product.ShelfLifeDays > 0)
            {
                expiryDate = DateTime.UtcNow.AddDays(product.ShelfLifeDays.Value);
            }

            var existing = await _context.Inventory
                .FirstOrDefaultAsync(i => i.ProductID == productId && i.CompanyID == companyId);

            if (existing != null)
            {
                existing.Quantity = (existing.Quantity ?? 0) + quantity;
                // Update expiry if product has shelf life configured
                if (expiryDate.HasValue)
                {
                    existing.Expiry = expiryDate;
                }
            }
            else
            {
                _context.Inventory.Add(new Inventory
                {
                    ProductID = productId,
                    CompanyID = companyId,
                    UserID = user?.Id ?? string.Empty,
                    Quantity = quantity,
                    Expiry = expiryDate
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
