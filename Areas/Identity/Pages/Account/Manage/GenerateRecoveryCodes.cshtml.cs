// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IT15_DairyFlow.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class GenerateRecoveryCodesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<GenerateRecoveryCodesModel> _logger;

        public GenerateRecoveryCodesModel(
            UserManager<ApplicationUser> userManager,
            ILogger<GenerateRecoveryCodesModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string[] RecoveryCodes { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!isTwoFactorEnabled)
            {
                return NotFound("Cannot generate recovery codes for user as they do not have 2FA enabled.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!isTwoFactorEnabled)
            {
                return NotFound("Cannot generate recovery codes for user as they do not have 2FA enabled.");
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            RecoveryCodes = recoveryCodes.ToArray();

            _logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", await _userManager.GetUserIdAsync(user));
            StatusMessage = "You have generated new recovery codes.";

            return RedirectToPage("./ShowRecoveryCodes");
        }
    }
}
