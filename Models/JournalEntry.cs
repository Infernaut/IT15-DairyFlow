using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class JournalEntry
    {
        [Key]
        public int JournalEntryID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        public DateTime? Date { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TotalDebit { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TotalCredit { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;
    }
}
