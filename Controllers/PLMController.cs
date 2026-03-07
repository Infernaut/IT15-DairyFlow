using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.PLM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Admin,ProductManager")]
    public class PLMController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PLMController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: PLM/Products
        [HttpGet]
        public async Task<IActionResult> Products(bool showArchived = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var companyId = GetUserCompanyId(user);

            var query = _context.Product
                .Where(p => p.CompanyID == companyId);

            // Filter out archived products unless showArchived is true
            if (!showArchived)
            {
                query = query.Where(p => p.LifecycleStatus != "Archived");
            }

            var products = await query
                .OrderBy(p => p.ProductName)
                .Select(p => new ProductListViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName ?? string.Empty,
                    Type = p.Type,
                    LifecycleStatus = p.LifecycleStatus,
                    CompanyID = p.CompanyID,
                    UserEmail = p.User.Email ?? string.Empty
                })
                .AsNoTracking()
                .ToListAsync();

            ViewBag.ShowArchived = showArchived;
            return View(products);
        }

        // GET: PLM/Archives
        [HttpGet]
        public async Task<IActionResult> Archives()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var companyId = GetUserCompanyId(user);

            var products = await _context.Product
                .Where(p => p.CompanyID == companyId &&
                    (p.LifecycleStatus == "Archived" || p.LifecycleStatus == "Archive"))
                .OrderBy(p => p.ProductName)
                .Select(p => new ProductListViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName ?? string.Empty,
                    Type = p.Type,
                    LifecycleStatus = p.LifecycleStatus,
                    CompanyID = p.CompanyID,
                    UserEmail = p.User.Email ?? string.Empty
                })
                .AsNoTracking()
                .ToListAsync();

            return View(products);
        }

        // GET: PLM/GetProductDetail/{id}
        [HttpGet]
        public async Task<IActionResult> GetProductDetail(int id)
        {
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));

            var viewModel = await _context.Product
                .Where(p => p.ProductID == id && p.CompanyID == companyId)
                .Select(p => new ProductDetailViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName ?? string.Empty,
                    Type = p.Type,
                    LifecycleStatus = p.LifecycleStatus,
                    CompanyID = p.CompanyID,
                    UserID = p.UserID,
                    UserEmail = p.User.Email ?? string.Empty
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (viewModel == null)
                return NotFound();

            return Json(viewModel);
        }

        // POST: PLM/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductViewModel model)
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

            var companyId = GetUserCompanyId(user);

            var product = new Product
            {
                ProductName = model.ProductName,
                Type = model.Type,
                LifecycleStatus = "underreview",
                CompanyID = companyId,
                UserID = user.Id
            };

            _context.Product.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product created successfully!" });
        }

        // POST: PLM/UpdateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct([FromBody] EditProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _context.Product.FindAsync(model.ProductID);
            if (product == null)
            {
                return NotFound();
            }

            // Check if product belongs to user's company
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));
            if (product.CompanyID != companyId)
            {
                return Forbid();
            }

            // Prevent editing approved products
            if (product.LifecycleStatus == "Approved")
            {
                return BadRequest(new { success = false, message = "Approved products cannot be edited." });
            }

            product.ProductName = model.ProductName;
            product.Type = model.Type;
            product.LifecycleStatus = model.LifecycleStatus;

            _context.Product.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product updated successfully!" });
        }

        // POST: PLM/ArchiveProduct/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveProduct(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Check if product belongs to user's company
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));
            if (product.CompanyID != companyId)
            {
                return Forbid();
            }

            product.LifecycleStatus = "Archived";
            _context.Product.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product archived successfully!" });
        }

        // POST: PLM/UnarchiveProduct/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveProduct(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));
            if (product.CompanyID != companyId)
            {
                return Forbid();
            }

            product.LifecycleStatus = "Active";
            _context.Product.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product unarchived successfully!" });
        }

        // GET: PLM/Formulation/{productId}
        [HttpGet]
        public async Task<IActionResult> Formulation(int productId)
        {
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));

            var product = await _context.Product
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.CompanyID == companyId);

            if (product == null)
            {
                return NotFound();
            }

            var ingredients = await _context.ProductFormulation
                .Where(f => f.ProductID == productId && f.CompanyID == companyId && f.IsActive)
                .OrderBy(f => f.ProcessOrder ?? int.MaxValue)
                .Select(f => new FormulationIngredientViewModel
                {
                    FormulationID = f.FormulationID,
                    RawMaterialID = f.RawMaterialID,
                    MaterialName = f.RawMaterial.MaterialName ?? "Unknown",
                    Quantity = f.Quantity,
                    Unit = f.Unit ?? "kg",
                    ProcessOrder = f.ProcessOrder,
                    ProcessInstructions = f.ProcessInstructions,
                    UnitCost = f.RawMaterial.UnitCost ?? 0
                })
                .AsNoTracking()
                .ToListAsync();

            var availableMaterials = await _context.RawMaterial
                .Where(m => m.CompanyID == companyId)
                .OrderBy(m => m.MaterialName)
                .Select(m => new RawMaterialLookupViewModel
                {
                    Id = m.RawMaterialID,
                    Name = m.MaterialName ?? "Unknown",
                    UnitCost = m.UnitCost ?? 0
                })
                .AsNoTracking()
                .ToListAsync();

            var viewModel = new FormulationPageViewModel
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName ?? "Unknown",
                ProductType = product.Type,
                LifecycleStatus = product.LifecycleStatus,
                Ingredients = ingredients,
                AvailableMaterials = availableMaterials,
                TotalCost = ingredients.Sum(i => i.TotalCost)
            };

            return View(viewModel);
        }

        // POST: PLM/AddIngredient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIngredient([FromBody] CreateFormulationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));

            // Verify product exists
            var product = await _context.Product
                .FirstOrDefaultAsync(p => p.ProductID == model.ProductID && p.CompanyID == companyId);

            if (product == null)
            {
                return BadRequest("Invalid product.");
            }

            // Prevent formulation changes for approved products
            if (product.LifecycleStatus == "Approved")
            {
                return BadRequest(new { success = false, message = "Cannot modify formulation for an approved product." });
            }

            // Verify raw material exists
            var rawMaterial = await _context.RawMaterial
                .FirstOrDefaultAsync(m => m.RawMaterialID == model.RawMaterialID && m.CompanyID == companyId);

            if (rawMaterial == null)
            {
                return BadRequest("Invalid raw material.");
            }

            // Check if ingredient already exists
            var existingFormulation = await _context.ProductFormulation
                .FirstOrDefaultAsync(f => f.ProductID == model.ProductID && 
                                         f.RawMaterialID == model.RawMaterialID && 
                                         f.CompanyID == companyId && 
                                         f.IsActive);

            if (existingFormulation != null)
            {
                // Update existing
                existingFormulation.Quantity += model.Quantity;
                existingFormulation.UpdatedAt = DateTime.UtcNow;
                _context.ProductFormulation.Update(existingFormulation);
            }
            else
            {
                // Create new
                var formulation = new ProductFormulation
                {
                    ProductID = model.ProductID,
                    CompanyID = companyId,
                    RawMaterialID = model.RawMaterialID,
                    Quantity = model.Quantity,
                    Unit = model.Unit ?? "kg",
                    ProcessOrder = model.ProcessOrder,
                    ProcessInstructions = model.ProcessInstructions,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductFormulation.Add(formulation);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Ingredient added successfully!" });
        }

        // POST: PLM/UpdateIngredient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIngredient([FromBody] UpdateFormulationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));

            var formulation = await _context.ProductFormulation
                .Include(f => f.Product)
                .FirstOrDefaultAsync(f => f.FormulationID == model.FormulationID && f.CompanyID == companyId);

            if (formulation == null)
            {
                return NotFound();
            }

            // Prevent formulation changes for approved products
            if (formulation.Product?.LifecycleStatus == "Approved")
            {
                return BadRequest(new { success = false, message = "Cannot modify formulation for an approved product." });
            }

            formulation.Quantity = model.Quantity;
            formulation.Unit = model.Unit ?? "kg";
            formulation.ProcessOrder = model.ProcessOrder;
            formulation.ProcessInstructions = model.ProcessInstructions;
            formulation.UpdatedAt = DateTime.UtcNow;

            _context.ProductFormulation.Update(formulation);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Ingredient updated successfully!" });
        }

        // POST: PLM/RemoveIngredient/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveIngredient(int id)
        {
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));

            var formulation = await _context.ProductFormulation
                .Include(f => f.Product)
                .FirstOrDefaultAsync(f => f.FormulationID == id && f.CompanyID == companyId);

            if (formulation == null)
            {
                return NotFound();
            }

            // Prevent formulation changes for approved products
            if (formulation.Product?.LifecycleStatus == "Approved")
            {
                return BadRequest(new { success = false, message = "Cannot modify formulation for an approved product." });
            }

            // Soft delete
            formulation.IsActive = false;
            formulation.UpdatedAt = DateTime.UtcNow;
            _context.ProductFormulation.Update(formulation);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Ingredient removed successfully!" });
        }

        // GET: PLM/GetFormulationDetail/{id}
        [HttpGet]
        public async Task<IActionResult> GetFormulationDetail(int id)
        {
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));

            var viewModel = await _context.ProductFormulation
                .Where(f => f.FormulationID == id && f.CompanyID == companyId)
                .Select(f => new FormulationIngredientViewModel
                {
                    FormulationID = f.FormulationID,
                    RawMaterialID = f.RawMaterialID,
                    MaterialName = f.RawMaterial.MaterialName ?? "Unknown",
                    Quantity = f.Quantity,
                    Unit = f.Unit ?? "kg",
                    ProcessOrder = f.ProcessOrder,
                    ProcessInstructions = f.ProcessInstructions,
                    UnitCost = f.RawMaterial.UnitCost ?? 0
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (viewModel == null)
                return NotFound();

            return Json(viewModel);
        }

        // GET: PLM/CalculateBatchRequirements/{productId}/{quantity}
        [HttpGet]
        public async Task<IActionResult> CalculateBatchRequirements(int productId, int quantity)
        {
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));

            var ingredients = await _context.ProductFormulation
                .Where(f => f.ProductID == productId && f.CompanyID == companyId && f.IsActive)
                .Select(f => new BatchMaterialRequirementViewModel
                {
                    MaterialName = f.RawMaterial.MaterialName ?? "Unknown",
                    RequiredQuantity = f.Quantity * quantity,
                    Unit = f.Unit ?? "kg",
                    UnitCost = f.RawMaterial.UnitCost ?? 0,
                    TotalCost = (f.Quantity * quantity) * (f.RawMaterial.UnitCost ?? 0)
                })
                .AsNoTracking()
                .ToListAsync();

            return Json(new 
            { 
                requirements = ingredients, 
                totalCost = ingredients.Sum(i => i.TotalCost) 
            });
        }

        // ─── PRODUCT APPROVAL (Admin Only) ─────────────────────

        // GET: PLM/Approvals
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approvals()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var companyId = GetUserCompanyId(user);

            var products = await _context.Product
                .Where(p => p.CompanyID == companyId && p.LifecycleStatus != "Archived")
                // Only show products that have at least one active formulation ingredient
                .Where(p => _context.ProductFormulation.Any(f => f.ProductID == p.ProductID && f.CompanyID == companyId && f.IsActive))
                .OrderByDescending(p => p.ProductID)
                .Select(p => new ProductApprovalListViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName ?? string.Empty,
                    Type = p.Type,
                    LifecycleStatus = p.LifecycleStatus,
                    CreatedByUserName = p.User.UserName ?? p.User.Email ?? "Unknown",
                    CreatedByEmail = p.User.Email ?? string.Empty
                })
                .AsNoTracking()
                .ToListAsync();

            return View(products);
        }

        // GET: PLM/GetApprovalDetail/{id}
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetApprovalDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var companyId = GetUserCompanyId(user);

            var product = await _context.Product
                .Include(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductID == id && p.CompanyID == companyId);

            if (product == null) return NotFound();

            var ingredients = await _context.ProductFormulation
                .Where(f => f.ProductID == id && f.CompanyID == companyId && f.IsActive)
                .OrderBy(f => f.ProcessOrder ?? int.MaxValue)
                .Select(f => new FormulationIngredientViewModel
                {
                    FormulationID = f.FormulationID,
                    RawMaterialID = f.RawMaterialID,
                    MaterialName = f.RawMaterial.MaterialName ?? "Unknown",
                    Quantity = f.Quantity,
                    Unit = f.Unit ?? "kg",
                    ProcessOrder = f.ProcessOrder,
                    ProcessInstructions = f.ProcessInstructions,
                    UnitCost = f.RawMaterial.UnitCost ?? 0
                })
                .AsNoTracking()
                .ToListAsync();

            var viewModel = new ProductApprovalDetailViewModel
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName ?? string.Empty,
                Type = product.Type,
                LifecycleStatus = product.LifecycleStatus,
                CreatedByUserName = product.User?.UserName ?? product.User?.Email ?? "Unknown",
                CreatedByEmail = product.User?.Email ?? string.Empty,
                Ingredients = ingredients,
                TotalUnitCost = ingredients.Sum(i => i.TotalCost)
            };

            return Json(viewModel);
        }

        // POST: PLM/ProcessApproval
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessApproval([FromBody] ProductApprovalActionViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var companyId = GetUserCompanyId(user);

            var product = await _context.Product
                .FirstOrDefaultAsync(p => p.ProductID == model.ProductID && p.CompanyID == companyId);

            if (product == null) return NotFound();

            if (product.LifecycleStatus == "Approved")
                return BadRequest(new { success = false, message = "This product is already approved and cannot be changed." });

            if (model.Action == "Approved")
            {
                product.LifecycleStatus = "Approved";
                _context.Product.Update(product);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Product has been approved." });
            }
            else
            {
                // Rejected / Needs Revision
                product.LifecycleStatus = "Needs Revision";
                _context.Product.Update(product);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = $"Product status set to Needs Revision. Reason: {model.Reason}" });
            }
        }

        // Helper method to get user's CompanyID
        // Defaults to 1 when not set, but can be extended to read from user claims or custom user properties
        private int GetUserCompanyId(ApplicationUser? user)
        {
            return user?.CompanyID ?? 1;
        }
    }
}
