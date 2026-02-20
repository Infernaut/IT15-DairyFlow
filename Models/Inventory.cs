using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IT15_DairyFlow.Models;

namespace IT15_DairyFlow.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserID { get; set; } = string.Empty;

        public int? Quantity { get; set; }

        public DateTime? Expiry { get; set; }

        // Navigation properties
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
