using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class SaleTransaction
    {
        [Key]
        public int TransactionID { get; set; }

        [Required]
        public int SaleID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Cash, Online (PayMongo)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(450)]
        public string ProcessedByUserID { get; set; } = string.Empty;

        /// <summary>
        /// PayMongo checkout session ID for online payments
        /// </summary>
        [MaxLength(256)]
        public string? PayMongoSessionId { get; set; }

        [MaxLength(100)]
    [NotMapped]
    public string? ReferenceNumber { get; set; }

    public string? ReferenceNumberEncrypted { get; set; }

        [MaxLength(500)]
    [NotMapped]
    public string? Notes { get; set; }

    public string? NotesEncrypted { get; set; }

        // Navigation properties
        [ForeignKey("SaleID")]
        public virtual Sale Sale { get; set; } = null!;

        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("ProcessedByUserID")]
        public virtual ApplicationUser ProcessedByUser { get; set; } = null!;
    }
}
