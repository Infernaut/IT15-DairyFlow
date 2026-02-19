using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class Subscription
    {
        [Key]
        public int SubscriptionID { get; set; }

        [MaxLength(100)]
        public string? PlanName { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Price { get; set; }

        [MaxLength(50)]
        public string? BillingCycle { get; set; }

        // Navigation property
        public virtual ICollection<Company> Companies { get; set; } = new List<Company>();
    }
}
