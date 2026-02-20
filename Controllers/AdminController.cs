using IT15_DairyFlow.Models.Admin;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const string SuperAdminRoleName = "Superadmin";
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var items = new List<UserListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isProtected = user.Id == currentUserId || roles.Contains(SuperAdminRoleName);
                items.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Roles = roles.Count > 0 ? string.Join(", ", roles) : "None",
                    IsActive = IsUserActive(user),
                    IsProtected = isProtected
                });
            }

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                IsActive = IsUserActive(user),
                IsProtected = user.Id == currentUserId || roles.Contains(SuperAdminRoleName),
                RoleSummary = roles.Count > 0 ? string.Join(", ", roles) : "None"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            var isProtected = user.Id == currentUserId || roles.Contains(SuperAdminRoleName);

            user.Email = model.Email;
            user.UserName = model.UserName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber;

            if (!isProtected)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = model.IsActive ? null : DateTimeOffset.MaxValue;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.IsProtected = isProtected;
                model.RoleSummary = roles.Count > 0 ? string.Join(", ", roles) : "None";
                return View(model);
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            if (user.Id == currentUserId || roles.Contains(SuperAdminRoleName))
            {
                return RedirectToAction(nameof(Users));
            }

            var isActive = IsUserActive(user);
            user.LockoutEnabled = true;
            user.LockoutEnd = isActive ? DateTimeOffset.MaxValue : null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var items = roles.Select(role => new RoleListItemViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty
            }).ToList();

            return View(items);
        }

        [HttpGet]
        public IActionResult CreateRole()
        {
            return View(new RoleEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(RoleEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _roleManager.RoleExistsAsync(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Role already exists.");
                return View(model);
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Roles));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return View(new RoleEditViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(RoleEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var role = await _roleManager.FindByIdAsync(model.Id);
            if (role == null)
            {
                return NotFound();
            }

            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Roles));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UserRoles(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var model = new UserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? "User",
                Roles = new List<UserRoleItemViewModel>()
            };

            foreach (var role in roles)
            {
                var roleName = role.Name ?? string.Empty;
                model.Roles.Add(new UserRoleItemViewModel
                {
                    RoleId = role.Id,
                    RoleName = roleName,
                    Selected = !string.IsNullOrEmpty(roleName) && await _userManager.IsInRoleAsync(user, roleName)
                });
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Logs()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserRoles(UserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var selectedRoles = model.Roles
                .Where(role => role.Selected)
                .Select(role => role.RoleName)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .ToList();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                model.Roles = roles.Select(role => new UserRoleItemViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    Selected = selectedRoles.Contains(role.Name ?? string.Empty)
                }).ToList();

                model.Email = user.Email ?? user.UserName ?? "User";
                return View(model);
            }

            return RedirectToAction(nameof(Users));
        }

        private static bool IsUserActive(ApplicationUser user)
        {
            if (!user.LockoutEnd.HasValue)
            {
                return true;
            }

            return user.LockoutEnd <= DateTimeOffset.UtcNow;
        }
    }
}
