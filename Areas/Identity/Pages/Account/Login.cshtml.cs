// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IT15_DairyFlow.Services.Security;
using IT15_DairyFlow.Security.Crypto;
using IT15_DairyFlow.Services.Security.Captcha;
using Microsoft.Extensions.Options;

namespace IT15_DairyFlow.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _audit;
    private readonly ICryptoService _crypto;
    private readonly ICaptchaVerificationService _captcha;
	private readonly IOptions<CaptchaSettings> _captchaOptions;
        private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IAuditService audit, ICryptoService crypto, ICaptchaVerificationService captcha, IOptions<CaptchaSettings> captchaOptions, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _dbContext = dbContext;
            _audit = audit;
            _crypto = crypto;
            _captcha = captcha;
			_captchaOptions = captchaOptions;
            _logger = logger;
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
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

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
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }

            // Google reCAPTCHA token (v2 checkbox uses implicit g-recaptcha-response)
            public string RecaptchaToken { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null, string email = null)
        {
            // If already logged in, redirect to home (prevents back-button to login)
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("/", new { area = "" });
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Pre-fill email if provided (e.g., from subscription registration)
            if (!string.IsNullOrEmpty(email))
            {
                Input = new InputModel { Email = email };
            }

            ReturnUrl = returnUrl;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // CAPTCHA validation (only when enabled). Google sends token in "g-recaptcha-response".
				if (_captchaOptions.Value.Enabled)
				{
					var captchaToken = Request.Form["g-recaptcha-response"].ToString();
					if (string.IsNullOrWhiteSpace(captchaToken))
					{
						captchaToken = Input.RecaptchaToken;
					}

					var captchaResult = await _captcha.VerifyAsync(captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString());
					if (!captchaResult.Success)
					{
						ModelState.AddModelError(string.Empty, captchaResult.Error ?? "CAPTCHA verification failed.");
						return Page();
					}
				}

                // Email-only: find user by deterministic email hash (email is stored encrypted in DB)
                var lookup = _crypto.ComputeLookupHash(Input.Email);
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmailLookupHash == lookup);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    await _audit.LogAsync(
                        module: "Identity",
                        actionType: "LoginSucceeded",
                        message: "User login succeeded.",
                        entityName: nameof(ApplicationUser),
                        entityId: user.Id,
                        companyIdOverride: user.CompanyID,
                        userIdOverride: user.Id);

                    // If user's company has no subscription yet, force plan selection
                    if (user.CompanyID.HasValue)
                    {
                        var company = await _dbContext.Company
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.CompanyID == user.CompanyID.Value);

                        if (company != null && company.SubscriptionID == null)
                        {
                            return Redirect("/Subscription/Plans");
                        }
                    }

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    await _audit.LogAsync(
                        module: "Identity",
                        actionType: "LoginRequiresTwoFactor",
                        message: "Login requires 2FA.",
                        entityName: nameof(ApplicationUser),
                        entityId: user.Id,
                        companyIdOverride: user.CompanyID,
                        userIdOverride: user.Id);
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");

                    await _audit.LogAsync(
                        module: "Identity",
                        actionType: "LoginLockedOut",
                        message: "Account locked out due to failed login attempts.",
                        entityName: nameof(ApplicationUser),
                        entityId: user.Id,
                        companyIdOverride: user.CompanyID,
                        userIdOverride: user.Id);
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    await _audit.LogAsync(
                        module: "Identity",
                        actionType: "LoginFailed",
                        message: "Invalid login attempt.",
                        entityName: nameof(ApplicationUser),
                        entityId: user.Id,
                        companyIdOverride: user.CompanyID,
                        userIdOverride: user.Id);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
