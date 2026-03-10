using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class Company
    {
        [Key]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(256)]
        public string CompanyName { get; set; } = string.Empty;

        public int? SubscriptionID { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        /// <summary>
        /// Date when the current subscription was activated. Used to calculate remaining days.
        /// </summary>
        public DateTime? SubscriptionStartDate { get; set; }

        // Navigation properties
        [ForeignKey("SubscriptionID")]
        public virtual Subscription? Subscription { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public virtual ICollection<RawMaterial> RawMaterials { get; set; } = new List<RawMaterial>();
        public virtual ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();
        public virtual ICollection<ProductionCost> ProductionCosts { get; set; } = new List<ProductionCost>();
        public virtual ICollection<QualityInspection> QualityInspection { get; set; } = new List<QualityInspection>();
        public virtual ICollection<NonConformance> NonConformances { get; set; } = new List<NonConformance>();
        public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public virtual ICollection<BillingInvoice> BillingInvoices { get; set; } = new List<BillingInvoice>();
        public virtual ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
