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

        [MaxLength(50)]
        public string? Period { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? AllocatedAmount { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;
    }
}
