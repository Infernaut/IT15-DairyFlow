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
    [Authorize]
    public class PLMController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PLMController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: PLM/Products
        [HttpGet]
        public async Task<IActionResult> Products(bool showArchived = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            // Get user's CompanyID (default to 0 if not set)
            // For now, we'll filter by the user's ID since CompanyID needs to be added to user
            var companyId = GetUserCompanyId();

            var query = _context.Products
                .Include(p => p.User)
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
                .ToListAsync();

            ViewBag.ShowArchived = showArchived;
            return View(products);
        }

        // GET: PLM/GetProductDetail/{id}
        [HttpGet]
        public async Task<IActionResult> GetProductDetail(int id)
        {
            var product = await _context.Products
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            // Check if product belongs to user's company
            var companyId = GetUserCompanyId();
            if (product.CompanyID != companyId)
            {
                return Forbid();
            }

            var viewModel = new ProductDetailViewModel
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName ?? string.Empty,
                Type = product.Type,
                LifecycleStatus = product.LifecycleStatus,
                CompanyID = product.CompanyID,
                UserID = product.UserID,
                UserEmail = product.User.Email ?? string.Empty
            };

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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var companyId = GetUserCompanyId();

            var product = new Product
            {
                ProductName = model.ProductName,
                Type = model.Type,
                LifecycleStatus = model.LifecycleStatus ?? "Active",
                CompanyID = companyId,
                UserID = userId
            };

            _context.Products.Add(product);
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

            var product = await _context.Products.FindAsync(model.ProductID);
            if (product == null)
            {
                return NotFound();
            }

            // Check if product belongs to user's company
            var companyId = GetUserCompanyId();
            if (product.CompanyID != companyId)
            {
                return Forbid();
            }

            product.ProductName = model.ProductName;
            product.Type = model.Type;
            product.LifecycleStatus = model.LifecycleStatus;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product updated successfully!" });
        }

        // POST: PLM/ArchiveProduct/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Check if product belongs to user's company
            var companyId = GetUserCompanyId();
            if (product.CompanyID != companyId)
            {
                return Forbid();
            }

            product.LifecycleStatus = "Archived";
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product archived successfully!" });
        }

        // Helper method to get user's CompanyID
        // For now returns 0, but can be extended to read from user claims or custom user properties
        private int GetUserCompanyId()
        {
            // TODO: Implement logic to get CompanyID from user
            // This could be from a claim, a custom user property, or a separate UserCompany table
            // For now, returning 0 as per requirement
            var companyIdClaim = User.FindFirstValue("CompanyID");
            if (!string.IsNullOrEmpty(companyIdClaim) && int.TryParse(companyIdClaim, out int companyId))
            {
                return companyId;
            }
            return 0;
        }
    }
}
