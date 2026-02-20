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

            var products = await _context.Products
                .Include(p => p.User)
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
                .ToListAsync();

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
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));
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
                LifecycleStatus = model.LifecycleStatus ?? "Active",
                CompanyID = companyId,
                UserID = user.Id
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
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));
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
            var companyId = GetUserCompanyId(await _userManager.GetUserAsync(User));
            if (product.CompanyID != companyId)
            {
                return Forbid();
            }

            product.LifecycleStatus = "Archived";
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product archived successfully!" });
        }

        // POST: PLM/UnarchiveProduct/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
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
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product unarchived successfully!" });
        }

        // Helper method to get user's CompanyID
        // Defaults to 1 when not set, but can be extended to read from user claims or custom user properties
        private int GetUserCompanyId(ApplicationUser? user)
        {
            return user?.CompanyID ?? 1;
        }
    }
}
