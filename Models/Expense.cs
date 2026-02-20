using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IT15_DairyFlow.Models;

namespace IT15_DairyFlow.Models
{
    public class Expense
    {
        [Key]
        public int ExpenseID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        public int? SupplierID { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserID { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Amount { get; set; }

        public DateTime? ExpenseDate { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("SupplierID")]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
