using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class Budget
    {
        [Key]
        public int BudgetID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        /// <summary>
        /// Month period in "YYYY-MM" format (e.g. "2026-03")
        /// </summary>
        [MaxLength(50)]
        public string? Period { get; set; }

        /// <summary>
        /// Budget category: Raw Materials, Equipment, Production, Other
        /// </summary>
        [MaxLength(50)]
        public string? Category { get; set; } = "Other";

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AllocatedAmount { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;
    }
}
