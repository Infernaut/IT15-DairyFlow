using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Sub;
using IT15_DairyFlow.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly PayMongoService _payMongoService;
        private readonly SubscriptionEmailService _emailService;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            PayMongoService payMongoService,
            SubscriptionEmailService emailService,
            ILogger<SubscriptionController> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _payMongoService = payMongoService;
            _emailService = emailService;
            _logger = logger;
        }

        // ─── Plan definitions ──────────────────────────────────────
        private static List<PlanDisplayItem> GetPlans()
        {
            return new List<PlanDisplayItem>
            {
                new()
                {
                    PlanId = "freetrial",
                    PlanName = "Free Trial",
                    BillingCycle = "14 Days",
                    Price = 0,
                    Description = "Try DairyFlow with full access for 14 days.",
                    Icon = "bi-hourglass-split",
                    BadgeText = "No Card Required",
                    IsFreeTrial = true,
                    Features = new List<string>
                    {
                        "Full ERP access for 14 days",
                        "All modules included",
                        "Up to 5 users",
                        "Email support",
                        "No credit card required"
                    }
                },
                new()
                {
                    PlanId = "monthly",
                    PlanName = "Monthly Plan",
                    BillingCycle = "Monthly",
                    Price = 2999,
                    Description = "Pay monthly with full flexibility.",
                    Icon = "bi-calendar-month",
                    BadgeText = "Flexible",
                    IsPopular = true,
                    Features = new List<string>
                    {
                        "Unlimited ERP access",
                        "All modules included",
                        "Unlimited users",
                        "Priority support",
                        "Monthly billing cycle"
                    }
                },
                new()
                {
                    PlanId = "annual",
                    PlanName = "Annual Plan",
                    BillingCycle = "Annual",
                    Price = 29990,
                    Description = "Save 17% with annual billing.",
                    Icon = "bi-calendar-check",
                    BadgeText = "Best Value",
                    Features = new List<string>
                    {
                        "Unlimited ERP access",
                        "All modules included",
                        "Unlimited users",
                        "Priority support",
                        "Save ₱5,998 per year"
                    }
                }
            };
        }

        // ─── GET: /Subscription/Register ───────────────────────────
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new SubscriptionRegisterViewModel());
        }

        // ─── POST: /Subscription/Register ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(SubscriptionRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists. Please log in instead.");
                return View(model);
            }

            // Create Company with SubscriptionID = null initially, Status = Pending
            var company = new Company
            {
                CompanyName = model.CompanyName,
                SubscriptionID = null,
                Status = "Pending"
            };

            _dbContext.Company.Add(company);
            await _dbContext.SaveChangesAsync();

            // Create ApplicationUser linked to the new company
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = true, // Auto-confirm since this is subscription registration
                CompanyID = company.CompanyID
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                // Rollback the company creation
                _dbContext.Company.Remove(company);
                await _dbContext.SaveChangesAsync();

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Assign user to Admin role
            await _userManager.AddToRoleAsync(user, "Admin");

            _logger.LogInformation("Subscription user {Email} created with CompanyID {CompanyId}", model.Email, company.CompanyID);

            // Store userId for the plan selection step
            TempData["SubUserId"] = user.Id;
            TempData["SubEmail"] = model.Email;

            return RedirectToAction("Plans");
        }

        // ─── GET: /Subscription/Plans ──────────────────────────────
        [HttpGet]
        public IActionResult Plans()
        {
            var userId = TempData["SubUserId"]?.ToString();
            var email = TempData["SubEmail"]?.ToString();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }

            // Keep TempData alive for next request
            TempData.Keep("SubUserId");
            TempData.Keep("SubEmail");

            var viewModel = new SubscriptionPlansViewModel
            {
                UserId = userId,
                Email = email,
                Plans = GetPlans()
            };

            return View(viewModel);
        }

        // ─── POST: /Subscription/SelectPlan ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectPlan(string userId, string email, string planId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(planId))
            {
                return RedirectToAction("Register");
            }

            var plan = GetPlans().FirstOrDefault(p => p.PlanId == planId);
            if (plan == null)
            {
                return RedirectToAction("Register");
            }

            // Map planId to SubscriptionID: freetrial=1, monthly=2, annual=3
            int subscriptionId = planId switch
            {
                "freetrial" => 1,
                "monthly" => 2,
                "annual" => 3,
                _ => 1
            };

            // Free Trial — skip payment, email directly
            if (plan.IsFreeTrial)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return RedirectToAction("Register");

                // Update company with subscription ID and status
                var company = await _dbContext.Company.FindAsync(user.CompanyID);
                if (company != null)
                {
                    company.SubscriptionID = subscriptionId;
                    company.Status = "FreeTrial";
                    await _dbContext.SaveChangesAsync();
                }

                // Send confirmation email
                await _emailService.SendSubscriptionConfirmationAsync(email, "Free Trial", true);

                return RedirectToAction("Success", new { email, planName = "Free Trial", isTrial = true });
            }

            // Paid plan — show payment method selection
            TempData["PayUserId"] = userId;
            TempData["PayEmail"] = email;
            TempData["PayPlanId"] = planId;
            TempData["PayPlanName"] = plan.PlanName;
            TempData["PayPrice"] = plan.Price.ToString("F2");
            TempData["PayBillingCycle"] = plan.BillingCycle;
            TempData["PaySubscriptionId"] = subscriptionId;

            return RedirectToAction("PaymentMethod");
        }

        // ─── GET: /Subscription/PaymentMethod ──────────────────────
        [HttpGet]
        public IActionResult PaymentMethod()
        {
            var userId = TempData["PayUserId"]?.ToString();
            var email = TempData["PayEmail"]?.ToString();
            var planId = TempData["PayPlanId"]?.ToString();
            var planName = TempData["PayPlanName"]?.ToString();
            var priceStr = TempData["PayPrice"]?.ToString();
            var billingCycle = TempData["PayBillingCycle"]?.ToString();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(planId))
            {
                return RedirectToAction("Register");
            }

            TempData.Keep("PayUserId");
            TempData.Keep("PayEmail");
            TempData.Keep("PayPlanId");
            TempData.Keep("PayPlanName");
            TempData.Keep("PayPrice");
            TempData.Keep("PayBillingCycle");

            var model = new PaymentMethodViewModel
            {
                UserId = userId!,
                Email = email!,
                PlanId = planId!,
                PlanName = planName!,
                Price = decimal.Parse(priceStr ?? "0"),
                BillingCycle = billingCycle!
            };

            return View(model);
        }

        // ─── POST: /Subscription/ProcessPayment ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(string paymentMethod)
        {
            var userId = TempData["PayUserId"]?.ToString();
            var email = TempData["PayEmail"]?.ToString();
            var planId = TempData["PayPlanId"]?.ToString();
            var planName = TempData["PayPlanName"]?.ToString();
            var priceStr = TempData["PayPrice"]?.ToString();
            var billingCycle = TempData["PayBillingCycle"]?.ToString();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(planId) || string.IsNullOrEmpty(paymentMethod))
            {
                return RedirectToAction("Register");
            }

            var price = decimal.Parse(priceStr ?? "0");
            var amountInCentavos = (int)(price * 100);

            // Map payment method to PayMongo type
            var payMongoType = paymentMethod.ToLower() switch
            {
                "card" => "card",
                "gcash" => "gcash",
                "paymaya" => "paymaya",
                _ => "card"
            };

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/Subscription/PaymentSuccess?userId={userId}&email={Uri.EscapeDataString(email!)}&planName={Uri.EscapeDataString(planName!)}&billingCycle={Uri.EscapeDataString(billingCycle!)}";
            var cancelUrl = $"{baseUrl}/Subscription/PaymentFailed?email={Uri.EscapeDataString(email!)}";

            var description = $"DairyFlow ERP — {planName} ({billingCycle})";

            var result = await _payMongoService.CreateCheckoutSession(
                email!,
                description,
                amountInCentavos,
                payMongoType,
                successUrl,
                cancelUrl);

            if (result == null)
            {
                TempData["ErrorMessage"] = "Unable to initialize payment. Please try again.";

                // Re-populate TempData for retry
                TempData["PayUserId"] = userId;
                TempData["PayEmail"] = email;
                TempData["PayPlanId"] = planId;
                TempData["PayPlanName"] = planName;
                TempData["PayPrice"] = priceStr;
                TempData["PayBillingCycle"] = billingCycle;

                return RedirectToAction("PaymentMethod");
            }

            // Store session ID for verification
            TempData[$"PayMongoSession_{userId}"] = result.Value.sessionId;

            // Redirect to PayMongo hosted checkout
            return Redirect(result.Value.checkoutUrl);
        }

        // ─── GET: /Subscription/PaymentSuccess ─────────────────────
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(string userId, string email, string planName, string billingCycle)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }

            // Verify payment with PayMongo (best effort)
            var sessionId = TempData[$"PayMongoSession_{userId}"]?.ToString();
            bool paymentVerified = false;

            if (!string.IsNullOrEmpty(sessionId))
            {
                paymentVerified = await _payMongoService.VerifyPayment(sessionId);
            }

            // Update company status and subscription ID
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var company = await _dbContext.Company.FindAsync(user.CompanyID);
                if (company != null)
                {
                    company.Status = "Active";

                    // Get subscription ID: monthly=2, annual=3
                    int subscriptionId = billingCycle.ToLower() switch
                    {
                        "monthly" => 2,
                        "annual" => 3,
                        _ => 2
                    };

                    company.SubscriptionID = subscriptionId;
                    await _dbContext.SaveChangesAsync();
                }
            }

            // Send confirmation email
            await _emailService.SendSubscriptionConfirmationAsync(email, planName ?? "Paid Plan", false);

            _logger.LogInformation("Payment success for {Email}, plan: {Plan}, verified: {Verified}", email, planName, paymentVerified);

            return RedirectToAction("Success", new { email, planName, isTrial = false });
        }

        // ─── GET: /Subscription/PaymentFailed ──────────────────────
        [HttpGet]
        public IActionResult PaymentFailed(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        // ─── GET: /Subscription/Success ────────────────────────────
        [HttpGet]
        public IActionResult Success(string email, string planName, bool isTrial)
        {
            var model = new SubscriptionSuccessViewModel
            {
                Email = email ?? "",
                PlanName = planName ?? "Subscription",
                IsFreeTrial = isTrial,
                Message = isTrial
                    ? "Your 14-day free trial has been activated! A confirmation email has been sent to your inbox."
                    : $"Your {planName} subscription is now active! A confirmation email has been sent to your inbox."
            };

            return View(model);
        }
    }
}
