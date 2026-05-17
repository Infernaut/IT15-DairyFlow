// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using IT15_DairyFlow.Security.Crypto;
using IT15_DairyFlow.Services.Security.Captcha;

namespace IT15_DairyFlow.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
    private readonly ICryptoService _crypto;
        private readonly ICaptchaVerificationService _captcha;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, ICryptoService crypto, ICaptchaVerificationService captcha)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _crypto = crypto;
            _captcha = captcha;
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

			// reCAPTCHA token (v3 uses explicit grecaptcha.execute)
			public string RecaptchaToken { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // CAPTCHA validation (if enabled)
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

                var lookup = _crypto.ComputeLookupHash(Input.Email);
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmailLookupHash == lookup);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Reset Password",
                    $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
