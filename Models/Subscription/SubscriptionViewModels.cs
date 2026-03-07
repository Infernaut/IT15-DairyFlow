using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.Sub
{
    public class SubscriptionRegisterViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class PlanDisplayItem
    {
        public string PlanId { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public bool IsFreeTrial { get; set; }
        public bool IsPopular { get; set; }
        public List<string> Features { get; set; } = new();
    }

    public class SubscriptionPlansViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<PlanDisplayItem> Plans { get; set; } = new();
    }

    public class PaymentMethodViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string BillingCycle { get; set; } = string.Empty;
    }

    public class SubscriptionSuccessViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public bool IsFreeTrial { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
