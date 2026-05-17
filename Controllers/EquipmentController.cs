using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Admin,ProductManager")]
    public class EquipmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EquipmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.CompanyID ?? 1;
        }

        // GET: Equipment/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var companyId = await GetCompanyIdAsync();
            var today = DateTime.UtcNow;

            var equipment = await _context.Equipment
                .Where(e => e.CompanyID == companyId)
                .OrderBy(e => e.EquipmentNameEncrypted)
                .Select(e => new EquipmentListViewModel
                {
                    EquipmentID = e.EquipmentID,
                    EquipmentName = e.EquipmentName,
                    EquipmentType = e.EquipmentType ?? "Other",
                    Location = e.Location ?? "Not Specified",
                    Status = e.Status ?? "Available",
                    Cost = e.Cost,
                    LastMaintenanceDate = e.LastMaintenanceDate,
                    DaysSinceLastMaintenance = e.LastMaintenanceDate.HasValue 
                        ? (int)(today - e.LastMaintenanceDate.Value).TotalDays 
                        : (int?)null,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            var summary = new EquipmentSummaryViewModel
            {
                TotalEquipment = equipment.Count,
                OperationalCount = equipment.Count(e => e.Status == "Available"),
                MaintenanceCount = equipment.Count(e => e.Status == "Under Maintenance"),
                OutOfServiceCount = equipment.Count(e => e.Status == "Out of Service"),
                NeedsMaintenanceCount = equipment.Count(e => e.DaysSinceLastMaintenance > 30)
            };

            var viewModel = new EquipmentPageViewModel
            {
                Equipment = equipment,
                Summary = summary
            };

            return View(viewModel);
        }

        // GET: Equipment/GetDetail/{id}
        [HttpGet]
        public async Task<IActionResult> GetDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var equipment = await _context.Equipment
                .FirstOrDefaultAsync(e => e.EquipmentID == id && e.CompanyID == companyId);

            if (equipment == null)
            {
                return NotFound();
            }

            var viewModel = new EquipmentDetailViewModel
            {
                EquipmentID = equipment.EquipmentID,
                EquipmentName = equipment.EquipmentName,
                EquipmentType = equipment.EquipmentType,
                Location = equipment.Location,
                Status = equipment.Status,
                Cost = equipment.Cost,
                LastMaintenanceDate = equipment.LastMaintenanceDate,
                CreatedAt = equipment.CreatedAt
            };

            return Json(viewModel);
        }

        // POST: Equipment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateEquipmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var equipment = new Equipment
            {
                CompanyID = companyId,
                EquipmentName = model.EquipmentName,
                EquipmentType = model.EquipmentType,
                Location = model.Location,
                Status = model.Status ?? "Available",
                Cost = model.Cost,
                LastMaintenanceDate = model.LastMaintenanceDate,
                CreatedAt = DateTime.UtcNow
            };

            _context.Equipment.Add(equipment);
            await _context.SaveChangesAsync();

            // Auto-create expense record for equipment purchase
            if (model.Cost.HasValue && model.Cost.Value > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                var expense = new Expense
                {
                    CompanyID = companyId,
                    UserID = user?.Id ?? string.Empty,
                    Amount = model.Cost.Value,
                    ExpenseDate = DateTime.UtcNow,
                    Category = "Equipment",
                    IsEmergency = model.IsEmergencyOverride,
                    EmergencyReason = model.IsEmergencyOverride ? model.EmergencyReason : null
                };
                _context.Expense.Add(expense);
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Equipment created successfully." });
        }

        // POST: Equipment/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] UpdateEquipmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var equipment = await _context.Equipment
                .FirstOrDefaultAsync(e => e.EquipmentID == model.EquipmentID && e.CompanyID == companyId);

            if (equipment == null)
            {
                return NotFound();
            }

            equipment.EquipmentName = model.EquipmentName;
            equipment.EquipmentType = model.EquipmentType;
            equipment.Location = model.Location;
            equipment.Cost = model.Cost;
            equipment.Status = model.Status;
            equipment.LastMaintenanceDate = model.LastMaintenanceDate;

            _context.Equipment.Update(equipment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Equipment updated successfully." });
        }

        // POST: Equipment/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var equipment = await _context.Equipment
                .FirstOrDefaultAsync(e => e.EquipmentID == id && e.CompanyID == companyId);

            if (equipment == null)
            {
                return NotFound();
            }

            // Check if equipment is in use by production batches
            var inUse = await _context.ProductionBatch
                .AnyAsync(pb => pb.EquipmentID == id);

            if (inUse)
            {
                return BadRequest("Cannot delete equipment that is assigned to production batches.");
            }

            _context.Equipment.Remove(equipment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Equipment deleted successfully." });
        }

        // POST: Equipment/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateEquipmentStatusViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var equipment = await _context.Equipment
                .FirstOrDefaultAsync(e => e.EquipmentID == model.EquipmentID && e.CompanyID == companyId);

            if (equipment == null)
            {
                return NotFound();
            }

            equipment.Status = model.Status;

            // If status is being set to Available from maintenance, update maintenance date
            if (model.Status == "Available" && equipment.Status == "Under Maintenance")
            {
                equipment.LastMaintenanceDate = DateTime.UtcNow;
            }

            _context.Equipment.Update(equipment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Equipment status updated to {model.Status}." });
        }

        // POST: Equipment/RecordMaintenance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordMaintenance([FromBody] RecordMaintenanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var equipment = await _context.Equipment
                .FirstOrDefaultAsync(e => e.EquipmentID == model.EquipmentID && e.CompanyID == companyId);

            if (equipment == null)
            {
                return NotFound();
            }

            equipment.LastMaintenanceDate = model.MaintenanceDate ?? DateTime.UtcNow;
            equipment.Status = "Available";

            _context.Equipment.Update(equipment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Maintenance recorded successfully." });
        }
    }

    // View Models for Equipment
    public class EquipmentPageViewModel
    {
        public List<EquipmentListViewModel> Equipment { get; set; } = new();
        public EquipmentSummaryViewModel Summary { get; set; } = new();
    }

    public class EquipmentSummaryViewModel
    {
        public int TotalEquipment { get; set; }
        public int OperationalCount { get; set; }
        public int MaintenanceCount { get; set; }
        public int OutOfServiceCount { get; set; }
        public int NeedsMaintenanceCount { get; set; }
    }

    public class EquipmentListViewModel
    {
        public int EquipmentID { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string EquipmentType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? Cost { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public int? DaysSinceLastMaintenance { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EquipmentDetailViewModel
    {
        public int EquipmentID { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string? EquipmentType { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public decimal? Cost { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateEquipmentViewModel
    {
        [Required]
        [MaxLength(200)]
        public string EquipmentName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EquipmentType { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; } = "Available";

        public decimal? Cost { get; set; }

        public DateTime? LastMaintenanceDate { get; set; }

        public bool IsEmergencyOverride { get; set; } = false;

        [MaxLength(500)]
        public string? EmergencyReason { get; set; }
    }

    public class UpdateEquipmentViewModel
    {
        [Required]
        public int EquipmentID { get; set; }

        [Required]
        [MaxLength(200)]
        public string EquipmentName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EquipmentType { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        public decimal? Cost { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        public DateTime? LastMaintenanceDate { get; set; }
    }

    public class UpdateEquipmentStatusViewModel
    {
        [Required]
        public int EquipmentID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;
    }

    public class RecordMaintenanceViewModel
    {
        [Required]
        public int EquipmentID { get; set; }

        public DateTime? MaintenanceDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
