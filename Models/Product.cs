using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IT15_DairyFlow.Models;

namespace IT15_DairyFlow.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        [Column("ProductID")]
        public int ProductID { get; set; }

        [Required]
        [Column("CompanyID")]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(450)]
        [Column("UserID")]
        public string UserID { get; set; } = string.Empty;

        [MaxLength(256)]
        [Column("productName")]
        public string? ProductName { get; set; }

        [MaxLength(100)]
        [Column("type")]
        public string? Type { get; set; }

        [MaxLength(50)]
        [Column("lifecycleStatus")]
        public string? LifecycleStatus { get; set; }

        /// <summary>
        /// Shelf life in days from production/release date.
        /// Used to auto-calculate product expiry date on QM release.
        /// Required for food safety compliance (FDA/BFAD).
        /// </summary>
        [Column("ShelfLifeDays")]
        public int? ShelfLifeDays { get; set; }

        /// <summary>
        /// Selling price per unit. Defaults to ingredient cost sum.
        /// Must not go below the sum of all product formulation ingredient costs.
        /// </summary>
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? UnitPrice { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
