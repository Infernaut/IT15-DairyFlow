using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class BillingInvoice
    {
        [Key]
        public int BillingInvoiceID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Amount { get; set; }

        public DateTime? DueDate { get; set; }

        [MaxLength(50)]
        public string? PaymentStatus { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;
    }
}
