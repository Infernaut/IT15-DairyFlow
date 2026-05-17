// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Security.Crypto;
using IT15_DairyFlow.Services.Security;

namespace IT15_DairyFlow.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPasswordHistoryService _passwordHistory;
        private readonly ICryptoService _crypto;
        private readonly CryptoSettings _cryptoSettings;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, IPasswordHistoryService passwordHistory, ICryptoService crypto, Microsoft.Extensions.Options.IOptions<CryptoSettings> cryptoSettings)
        {
            _userManager = userManager;
            _passwordHistory = passwordHistory;
            _crypto = crypto;
            _cryptoSettings = cryptoSettings.Value;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            public string Code { get; set; }

        }

        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }
            else
            {
                Input = new InputModel
                {
                    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
                };
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var lookup = _crypto.ComputeLookupHash(Input.Email);
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmailLookupHash == lookup);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // Prevent password reuse (last N)
            var ok = await _passwordHistory.IsPasswordReusedAsync(user.Id, Input.Password, _cryptoSettings.PasswordReuseHistoryDepth);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, $"You can't reuse your last {_cryptoSettings.PasswordReuseHistoryDepth} passwords.");
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                // Record new password hash for reuse prevention
                if (!string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    await _passwordHistory.RecordPasswordHashAsync(user.Id, user.PasswordHash, _cryptoSettings.PasswordReuseHistoryDepth);
                }
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}
