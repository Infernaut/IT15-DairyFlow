using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class Sale
    {
        [Key]
        public int SaleID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        public int InventoryID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        [MaxLength(256)]
    [NotMapped]
    public string BuyerName { get; set; } = string.Empty;

    public string? BuyerNameEncrypted { get; set; }
            public string? BuyerNameLookupHash { get; set; }

        [MaxLength(256)]
    [NotMapped]
    public string? BuyerEmail { get; set; }

    public string? BuyerEmailEncrypted { get; set; }
            public string? BuyerEmailLookupHash { get; set; }

        [MaxLength(50)]
    [NotMapped]
    public string? BuyerPhone { get; set; }

    public string? BuyerPhoneEncrypted { get; set; }
            public string? BuyerPhoneLookupHash { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(30)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Pending, Paid, Cancelled
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        /// <summary>
        /// Cash, Online (PayMongo)
        /// </summary>
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [Required]
        [MaxLength(450)]
        public string CreatedByUserID { get; set; } = string.Empty;

        [MaxLength(500)]
    [NotMapped]
    public string? Notes { get; set; }

    public string? NotesEncrypted { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("InventoryID")]
        public virtual Inventory Inventory { get; set; } = null!;

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("CreatedByUserID")]
        public virtual ApplicationUser CreatedByUser { get; set; } = null!;

        public virtual ICollection<SaleTransaction> Transactions { get; set; } = new List<SaleTransaction>();
    }
}
