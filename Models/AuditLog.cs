using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IT15_DairyFlow.Models;

namespace IT15_DairyFlow.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserID { get; set; } = string.Empty;

        public string? Action { get; set; }

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
