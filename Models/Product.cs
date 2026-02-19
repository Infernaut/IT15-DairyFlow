using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace IT15_DairyFlow.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserID { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? ProductName { get; set; }

        [MaxLength(100)]
        public string? Type { get; set; }

        [MaxLength(50)]
        public string? LifecycleStatus { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("UserID")]
        public virtual IdentityUser User { get; set; } = null!;

        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
