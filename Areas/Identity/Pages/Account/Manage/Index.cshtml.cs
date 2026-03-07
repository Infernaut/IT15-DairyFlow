#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IT15_DairyFlow.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public string Email { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Username is required.")]
            [StringLength(50, ErrorMessage = "Username must be at most {1} characters.")]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [Phone(ErrorMessage = "Please enter a valid phone number.")]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            Email = await _userManager.GetEmailAsync(user);

            Input = new InputModel
            {
                UserName = userName,
                PhoneNumber = phoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Update username if changed
            var currentUserName = await _userManager.GetUserNameAsync(user);
            if (Input.UserName != currentUserName)
            {
                var setResult = await _userManager.SetUserNameAsync(user, Input.UserName);
                if (!setResult.Succeeded)
                {
                    foreach (var error in setResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }
            }

            // Update phone if changed
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Error: Unable to set phone number.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
    }
}
