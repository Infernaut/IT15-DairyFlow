using IT15_DairyFlow.Data;
using IT15_DairyFlow.Hubs;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.InventoryVM;
using IT15_DairyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Admin,ProductManager,QualityChecker")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<DairyFlowHub> _hub;
        private readonly NotificationService _notificationService;
        private const int LowStockThreshold = 10;
        private const int ExpiringSoonDays = 7;

        public InventoryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<DairyFlowHub> hub, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _hub = hub;
            _notificationService = notificationService;
        }

        private async Task<int> GetCompanyIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.CompanyID ?? 1;
        }

        // GET: Inventory/Index (Finished Goods)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var companyId = await GetCompanyIdAsync();
            var today = DateTime.UtcNow.Date;
            var expiringSoonDate = today.AddDays(ExpiringSoonDays);

            // Pre-compute ingredient costs per product for minimum unit price
            var ingredientCosts = await _context.ProductFormulation
                .Include(f => f.RawMaterial)
                .Where(f => f.CompanyID == companyId && f.IsActive)
                .GroupBy(f => f.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    TotalCost = g.Sum(f => f.Quantity * (f.RawMaterial.UnitCost ?? 0))
                })
                .ToDictionaryAsync(x => x.ProductID, x => x.TotalCost);

            var inventoryItems = await _context.Inventory
                .Include(i => i.Product)
                .Include(i => i.User)
                .Where(i => i.CompanyID == companyId)
                .OrderBy(i => i.Product.ProductName)
                .Select(i => new InventoryListViewModel
                {
                    InventoryID = i.InventoryID,
                    ProductID = i.ProductID,
                    ProductName = i.Product.ProductName ?? "Unknown",
                    ProductType = i.Product.Type ?? "Other",
                    Quantity = i.Quantity ?? 0,
                    Expiry = i.Expiry,
                    UserName = i.User.UserName ?? i.User.Email ?? "Unknown",
                    StockStatus = (i.Quantity ?? 0) <= 0 ? "Out of Stock" :
                                  (i.Quantity ?? 0) < LowStockThreshold ? "Low Stock" : "In Stock",
                    IsExpiringSoon = i.Expiry.HasValue && i.Expiry.Value.Date <= expiringSoonDate && i.Expiry.Value.Date > today,
                    IsExpired = i.Expiry.HasValue && i.Expiry.Value.Date <= today,
                    DaysUntilExpiry = i.Expiry.HasValue ? (int)(i.Expiry.Value.Date - today).TotalDays : int.MaxValue,
                    UnitPrice = i.Product.UnitPrice ?? 0
                })
                .ToListAsync();

            // Populate MinUnitPrice from ingredient costs
            foreach (var item in inventoryItems)
            {
                item.MinUnitPrice = ingredientCosts.TryGetValue(item.ProductID, out var cost) ? cost : 0;
                // If unit price not yet set, default to ingredient cost
                if (item.UnitPrice == 0 && item.MinUnitPrice > 0)
                    item.UnitPrice = item.MinUnitPrice;
            }

            var products = await _context.Product
                .Where(p => p.CompanyID == companyId && 
                       (p.LifecycleStatus == "Active" || p.LifecycleStatus == "approved" || p.LifecycleStatus == "Produced"))
                .OrderBy(p => p.ProductName)
                .Select(p => new ProductLookupViewModel
                {
                    Id = p.ProductID,
                    Name = p.ProductName ?? "Unknown",
                    Type = p.Type ?? "Other"
                })
                .ToListAsync();

            var summary = new InventorySummaryViewModel
            {
                TotalItems = inventoryItems.Count,
                TotalQuantity = inventoryItems.Sum(i => i.Quantity),
                LowStockItems = inventoryItems.Count(i => i.StockStatus == "Low Stock" || i.StockStatus == "Out of Stock"),
                ExpiringSoonItems = inventoryItems.Count(i => i.IsExpiringSoon),
                ExpiredItems = inventoryItems.Count(i => i.IsExpired)
            };

            var viewModel = new InventoryPageViewModel
            {
                Items = inventoryItems,
                Products = products,
                Summary = summary
            };

            return View(viewModel);
        }

        // GET: Inventory/GetDetail/{id}
        [HttpGet]
        public async Task<IActionResult> GetDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var inventory = await _context.Inventory
                .Include(i => i.Product)
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.InventoryID == id && i.CompanyID == companyId);

            if (inventory == null)
            {
                return NotFound();
            }

            var viewModel = new InventoryDetailViewModel
            {
                InventoryID = inventory.InventoryID,
                ProductID = inventory.ProductID,
                ProductName = inventory.Product?.ProductName ?? "Unknown",
                ProductType = inventory.Product?.Type ?? "Other",
                Quantity = inventory.Quantity ?? 0,
                Expiry = inventory.Expiry,
                UserName = inventory.User?.UserName ?? inventory.User?.Email ?? "Unknown",
                UserID = inventory.UserID
            };

            return Json(viewModel);
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateInventoryViewModel model)
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

            var companyId = await GetCompanyIdAsync();

            // Check if product exists and belongs to the company
            var product = await _context.Product
                .FirstOrDefaultAsync(p => p.ProductID == model.ProductID && p.CompanyID == companyId);

            if (product == null)
            {
                return BadRequest("Invalid product selection.");
            }

            // Check if inventory already exists for this product
            var existingInventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.ProductID == model.ProductID && i.CompanyID == companyId);

            if (existingInventory != null)
            {
                // Update existing inventory instead of creating new
                existingInventory.Quantity = (existingInventory.Quantity ?? 0) + model.Quantity;
                if (model.Expiry.HasValue)
                {
                    existingInventory.Expiry = model.Expiry;
                }
                _context.Inventory.Update(existingInventory);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Inventory updated successfully. Added to existing stock." });
            }

            var inventory = new Inventory
            {
                ProductID = model.ProductID,
                CompanyID = companyId,
                UserID = user.Id,
                Quantity = model.Quantity,
                Expiry = model.Expiry
            };

            _context.Inventory.Add(inventory);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Inventory item created successfully." });
        }

        // POST: Inventory/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] UpdateInventoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var inventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.InventoryID == model.InventoryID && i.CompanyID == companyId);

            if (inventory == null)
            {
                return NotFound();
            }

            inventory.Quantity = model.Quantity;
            inventory.Expiry = model.Expiry;

            _context.Inventory.Update(inventory);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Inventory updated successfully." });
        }

        // POST: Inventory/Adjust
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust([FromBody] AdjustInventoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var inventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.InventoryID == model.InventoryID && i.CompanyID == companyId);

            if (inventory == null)
            {
                return NotFound();
            }

            var currentQuantity = inventory.Quantity ?? 0;
            var newQuantity = model.AdjustmentType switch
            {
                "Add" => currentQuantity + Math.Abs(model.AdjustmentQuantity),
                "Remove" => Math.Max(0, currentQuantity - Math.Abs(model.AdjustmentQuantity)),
                "Damaged" => Math.Max(0, currentQuantity - Math.Abs(model.AdjustmentQuantity)),
                "Expired" => Math.Max(0, currentQuantity - Math.Abs(model.AdjustmentQuantity)),
                _ => currentQuantity
            };

            inventory.Quantity = newQuantity;
            _context.Inventory.Update(inventory);
            await _context.SaveChangesAsync();

            // SignalR: notify about inventory adjustment
            var user = await _userManager.GetUserAsync(User);
            await _hub.NotifyInventoryUpdated(companyId, model.AdjustmentType, $"Item #{model.InventoryID}", user?.UserName ?? "");
            if (newQuantity < LowStockThreshold)
            {
                await _hub.NotifyLowStock(companyId, $"Item #{model.InventoryID}", newQuantity, LowStockThreshold);
            }
            await _hub.NotifyDashboardRefresh(companyId, "Inventory");
            await _notificationService.NotifyCompanyActionAsync(
                user?.Id ?? "", companyId, $"Inventory {model.AdjustmentType}: Item #{model.InventoryID} (qty: {newQuantity})", "Inventory", "bi-box-seam");

            return Ok(new { 
                success = true, 
                message = $"Inventory adjusted successfully. New quantity: {newQuantity}",
                newQuantity = newQuantity
            });
        }

        // POST: Inventory/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var inventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.InventoryID == id && i.CompanyID == companyId);

            if (inventory == null)
            {
                return NotFound();
            }

            _context.Inventory.Remove(inventory);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Inventory item deleted successfully." });
        }

        // GET: Inventory/GetProductPriceInfo/{productId}
        [HttpGet]
        public async Task<IActionResult> GetProductPriceInfo(int productId)
        {
            var companyId = await GetCompanyIdAsync();

            var product = await _context.Product
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.CompanyID == companyId);

            if (product == null)
                return NotFound();

            var formulations = await _context.ProductFormulation
                .Include(f => f.RawMaterial)
                .Where(f => f.ProductID == productId && f.CompanyID == companyId && f.IsActive)
                .ToListAsync();

            var ingredients = formulations.Select(f => new IngredientCostViewModel
            {
                MaterialName = f.RawMaterial?.MaterialName ?? "Unknown",
                Quantity = f.Quantity,
                Unit = f.Unit ?? "kg",
                UnitCost = f.RawMaterial?.UnitCost ?? 0,
                TotalCost = f.Quantity * (f.RawMaterial?.UnitCost ?? 0)
            }).ToList();

            var minPrice = ingredients.Sum(i => i.TotalCost);

            return Json(new ProductPriceInfoViewModel
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName ?? "Unknown",
                CurrentUnitPrice = product.UnitPrice ?? minPrice,
                MinUnitPrice = minPrice,
                Ingredients = ingredients
            });
        }

        // POST: Inventory/SetUnitPrice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUnitPrice([FromBody] SetUnitPriceViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var companyId = await GetCompanyIdAsync();

            var product = await _context.Product
                .FirstOrDefaultAsync(p => p.ProductID == model.ProductID && p.CompanyID == companyId);

            if (product == null)
                return NotFound("Product not found.");

            // Calculate minimum price from ingredients
            var minPrice = await _context.ProductFormulation
                .Include(f => f.RawMaterial)
                .Where(f => f.ProductID == model.ProductID && f.CompanyID == companyId && f.IsActive)
                .SumAsync(f => f.Quantity * (f.RawMaterial.UnitCost ?? 0));

            if (model.UnitPrice < minPrice)
            {
                return BadRequest(new
                {
                    error = $"Unit price (₱{model.UnitPrice:N2}) cannot be lower than ingredient cost (₱{minPrice:N2})."
                });
            }

            product.UnitPrice = model.UnitPrice;
            _context.Product.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Unit price for {product.ProductName} set to ₱{model.UnitPrice:N2}.",
                unitPrice = model.UnitPrice
            });
        }

        // GET: Inventory/LowStock
        [HttpGet]
        public async Task<IActionResult> LowStock()
        {
            var companyId = await GetCompanyIdAsync();

            var lowStockItems = await _context.Inventory
                .Include(i => i.Product)
                .Include(i => i.User)
                .Where(i => i.CompanyID == companyId && i.Quantity < LowStockThreshold)
                .OrderBy(i => i.Quantity)
                .Select(i => new InventoryListViewModel
                {
                    InventoryID = i.InventoryID,
                    ProductID = i.ProductID,
                    ProductName = i.Product.ProductName ?? "Unknown",
                    ProductType = i.Product.Type ?? "Other",
                    Quantity = i.Quantity ?? 0,
                    Expiry = i.Expiry,
                    StockStatus = (i.Quantity ?? 0) <= 0 ? "Out of Stock" : "Low Stock"
                })
                .ToListAsync();

            return Json(lowStockItems);
        }

        // GET: Inventory/ExpiringSoon
        [HttpGet]
        public async Task<IActionResult> ExpiringSoon()
        {
            var companyId = await GetCompanyIdAsync();
            var today = DateTime.UtcNow.Date;
            var expiringSoonDate = today.AddDays(ExpiringSoonDays);

            var expiringSoonItems = await _context.Inventory
                .Include(i => i.Product)
                .Include(i => i.User)
                .Where(i => i.CompanyID == companyId && 
                       i.Expiry.HasValue && 
                       i.Expiry.Value.Date <= expiringSoonDate &&
                       i.Expiry.Value.Date > today)
                .OrderBy(i => i.Expiry)
                .Select(i => new InventoryListViewModel
                {
                    InventoryID = i.InventoryID,
                    ProductID = i.ProductID,
                    ProductName = i.Product.ProductName ?? "Unknown",
                    ProductType = i.Product.Type ?? "Other",
                    Quantity = i.Quantity ?? 0,
                    Expiry = i.Expiry,
                    DaysUntilExpiry = i.Expiry.HasValue ? (int)(i.Expiry.Value.Date - today).TotalDays : int.MaxValue
                })
                .ToListAsync();

            return Json(expiringSoonItems);
        }

        // =====================================================
        // RAW MATERIALS MANAGEMENT
        // =====================================================

        // GET: Inventory/RawMaterials
        [HttpGet]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> RawMaterials()
        {
            var companyId = await GetCompanyIdAsync();

            var materials = await _context.RawMaterial
                .Include(m => m.Supplier)
                .Where(m => m.CompanyID == companyId)
                .OrderBy(m => m.MaterialName)
                .Select(m => new RawMaterialListViewModel
                {
                    RawMaterialID = m.RawMaterialID,
                    MaterialName = m.MaterialName ?? "Unknown",
                    SupplierID = m.SupplierID,
                    SupplierName = m.Supplier.SupplierName ?? "Unknown",
                    UnitCost = m.UnitCost ?? 0,
                    Unit = m.Unit ?? "kg",
                    CurrentStock = m.CurrentStock ?? 0,
                    MinimumStock = m.MinimumStock ?? 10,
                    LastRestockDate = m.LastRestockDate,
                    StockStatus = (m.CurrentStock ?? 0) <= 0 ? "Out of Stock" :
                                  (m.CurrentStock ?? 0) < (m.MinimumStock ?? 10) ? "Low Stock" : "In Stock"
                })
                .ToListAsync();

            var suppliers = await _context.Supplier
                .Where(s => s.CompanyID == companyId)
                .OrderBy(s => s.SupplierName)
                .Select(s => new SupplierSelectViewModel
                {
                    SupplierID = s.SupplierID,
                    SupplierName = s.SupplierName ?? "Unknown"
                })
                .ToListAsync();

            // Get products that have formulations for the "used in" info
            var formulationUsage = await _context.ProductFormulation
                .Include(f => f.Product)
                .Where(f => f.CompanyID == companyId && f.IsActive)
                .GroupBy(f => f.RawMaterialID)
                .Select(g => new
                {
                    RawMaterialID = g.Key,
                    ProductCount = g.Select(f => f.ProductID).Distinct().Count()
                })
                .ToDictionaryAsync(x => x.RawMaterialID, x => x.ProductCount);

            foreach (var material in materials)
            {
                material.UsedInProductCount = formulationUsage.TryGetValue(material.RawMaterialID, out var count) ? count : 0;
            }

            var summary = new RawMaterialSummaryViewModel
            {
                TotalMaterials = materials.Count,
                LowStockCount = materials.Count(m => m.StockStatus == "Low Stock"),
                OutOfStockCount = materials.Count(m => m.StockStatus == "Out of Stock"),
                TotalValue = materials.Sum(m => m.CurrentStock * m.UnitCost)
            };

            var viewModel = new RawMaterialPageViewModel
            {
                Materials = materials,
                Suppliers = suppliers,
                Summary = summary
            };

            return View(viewModel);
        }

        // GET: Inventory/GetRawMaterialDetail/{id}
        [HttpGet]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> GetRawMaterialDetail(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var material = await _context.RawMaterial
                .Include(m => m.Supplier)
                .FirstOrDefaultAsync(m => m.RawMaterialID == id && m.CompanyID == companyId);

            if (material == null)
            {
                return NotFound();
            }

            var viewModel = new RawMaterialDetailViewModel
            {
                RawMaterialID = material.RawMaterialID,
                MaterialName = material.MaterialName ?? "",
                SupplierID = material.SupplierID,
                SupplierName = material.Supplier?.SupplierName ?? "Unknown",
                UnitCost = material.UnitCost ?? 0,
                Unit = material.Unit ?? "kg",
                CurrentStock = material.CurrentStock ?? 0,
                MinimumStock = material.MinimumStock ?? 10,
                LastRestockDate = material.LastRestockDate
            };

            return Json(viewModel);
        }

        // POST: Inventory/CreateRawMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> CreateRawMaterial([FromBody] CreateRawMaterialViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            // Verify supplier exists
            var supplier = await _context.Supplier
                .FirstOrDefaultAsync(s => s.SupplierID == model.SupplierID && s.CompanyID == companyId);

            if (supplier == null)
            {
                return BadRequest("Invalid supplier selection.");
            }

            var material = new RawMaterial
            {
                CompanyID = companyId,
                SupplierID = model.SupplierID,
                MaterialName = model.MaterialName,
                UnitCost = model.UnitCost,
                Unit = model.Unit ?? "kg",
                CurrentStock = model.CurrentStock ?? 0,
                MinimumStock = model.MinimumStock ?? 10,
                LastRestockDate = model.CurrentStock > 0 ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.RawMaterial.Add(material);
            await _context.SaveChangesAsync();

            // Auto-create expense when raw material has initial stock and unit cost
            if (material.CurrentStock > 0 && material.UnitCost.HasValue)
            {
                var user = await _userManager.GetUserAsync(User);
                var expense = new Expense
                {
                    CompanyID = companyId,
                    SupplierID = material.SupplierID,
                    UserID = user!.Id,
                    Amount = material.CurrentStock.Value * material.UnitCost.Value,
                    ExpenseDate = DateTime.UtcNow,
                    Category = "Raw Materials",
                    IsEmergency = model.IsEmergencyOverride,
                    EmergencyReason = model.IsEmergencyOverride ? model.EmergencyReason : null
                };
                _context.Expense.Add(expense);
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Raw material created successfully." });
        }

        // POST: Inventory/UpdateRawMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> UpdateRawMaterial([FromBody] UpdateRawMaterialViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var material = await _context.RawMaterial
                .FirstOrDefaultAsync(m => m.RawMaterialID == model.RawMaterialID && m.CompanyID == companyId);

            if (material == null)
            {
                return NotFound();
            }

            material.MaterialName = model.MaterialName;
            material.SupplierID = model.SupplierID;
            material.UnitCost = model.UnitCost;
            material.Unit = model.Unit;
            material.MinimumStock = model.MinimumStock;

            _context.RawMaterial.Update(material);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Raw material updated successfully." });
        }

        // POST: Inventory/RestockRawMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestockRawMaterial([FromBody] RestockRawMaterialViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var material = await _context.RawMaterial
                .FirstOrDefaultAsync(m => m.RawMaterialID == model.RawMaterialID && m.CompanyID == companyId);

            if (material == null)
            {
                return NotFound();
            }

            material.CurrentStock = (material.CurrentStock ?? 0) + model.Quantity;
            material.LastRestockDate = DateTime.UtcNow;

            // Create an expense for the restock
            if (model.CreateExpense && material.UnitCost.HasValue)
            {
                var user = await _userManager.GetUserAsync(User);
                var expense = new Expense
                {
                    CompanyID = companyId,
                    SupplierID = material.SupplierID,
                    UserID = user!.Id,
                    Amount = model.Quantity * material.UnitCost.Value,
                    ExpenseDate = DateTime.UtcNow,
                    Category = "Raw Materials",
                    IsEmergency = model.IsEmergencyOverride,
                    EmergencyReason = model.IsEmergencyOverride ? model.EmergencyReason : null
                };
                _context.Expense.Add(expense);
            }

            _context.RawMaterial.Update(material);
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = $"Restocked {model.Quantity} {material.Unit}. New stock: {material.CurrentStock}",
                newStock = material.CurrentStock
            });
        }

        // POST: Inventory/ConsumeRawMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConsumeRawMaterial([FromBody] ConsumeRawMaterialViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = await GetCompanyIdAsync();

            var material = await _context.RawMaterial
                .FirstOrDefaultAsync(m => m.RawMaterialID == model.RawMaterialID && m.CompanyID == companyId);

            if (material == null)
            {
                return NotFound();
            }

            var currentStock = material.CurrentStock ?? 0;
            if (model.Quantity > currentStock)
            {
                return BadRequest($"Insufficient stock. Available: {currentStock} {material.Unit}");
            }

            material.CurrentStock = currentStock - model.Quantity;
            _context.RawMaterial.Update(material);
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = $"Consumed {model.Quantity} {material.Unit}. Remaining: {material.CurrentStock}",
                newStock = material.CurrentStock
            });
        }

        // POST: Inventory/DeleteRawMaterial/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> DeleteRawMaterial(int id)
        {
            var companyId = await GetCompanyIdAsync();

            var material = await _context.RawMaterial
                .FirstOrDefaultAsync(m => m.RawMaterialID == id && m.CompanyID == companyId);

            if (material == null)
            {
                return NotFound();
            }

            // Check if used in any formulation
            var inUse = await _context.ProductFormulation
                .AnyAsync(f => f.RawMaterialID == id && f.IsActive);

            if (inUse)
            {
                return BadRequest("Cannot delete raw material that is used in product formulations.");
            }

            _context.RawMaterial.Remove(material);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Raw material deleted successfully." });
        }
    }

    // View Models for Raw Materials
    public class RawMaterialPageViewModel
    {
        public List<RawMaterialListViewModel> Materials { get; set; } = new();
        public List<SupplierSelectViewModel> Suppliers { get; set; } = new();
        public RawMaterialSummaryViewModel Summary { get; set; } = new();
    }

    public class RawMaterialSummaryViewModel
    {
        public int TotalMaterials { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class RawMaterialListViewModel
    {
        public int RawMaterialID { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
        public string Unit { get; set; } = "kg";
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public DateTime? LastRestockDate { get; set; }
        public string StockStatus { get; set; } = "In Stock";
        public int UsedInProductCount { get; set; }
    }

    public class RawMaterialDetailViewModel
    {
        public int RawMaterialID { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
        public string Unit { get; set; } = "kg";
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public DateTime? LastRestockDate { get; set; }
    }

    public class SupplierSelectViewModel
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
    }

    public class CreateRawMaterialViewModel
    {
        [Required]
        [MaxLength(256)]
        public string MaterialName { get; set; } = string.Empty;

        [Required]
        public int SupplierID { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UnitCost { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; } = "kg";

        [Range(0, int.MaxValue)]
        public int? CurrentStock { get; set; }

        [Range(0, int.MaxValue)]
        public int? MinimumStock { get; set; } = 10;

        public bool IsEmergencyOverride { get; set; } = false;

        [MaxLength(500)]
        public string? EmergencyReason { get; set; }
    }

    public class UpdateRawMaterialViewModel
    {
        [Required]
        public int RawMaterialID { get; set; }

        [Required]
        [MaxLength(256)]
        public string MaterialName { get; set; } = string.Empty;

        [Required]
        public int SupplierID { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UnitCost { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        [Range(0, int.MaxValue)]
        public int? MinimumStock { get; set; }
    }

    public class RestockRawMaterialViewModel
    {
        [Required]
        public int RawMaterialID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        public bool CreateExpense { get; set; } = true;

        public bool IsEmergencyOverride { get; set; } = false;

        [MaxLength(500)]
        public string? EmergencyReason { get; set; }
    }

    public class ConsumeRawMaterialViewModel
    {
        [Required]
        public int RawMaterialID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
