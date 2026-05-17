using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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

        [MaxLength(128)]
        public string ActionType { get; set; } = string.Empty; // e.g. LoginSucceeded, InventoryUpdated

        [MaxLength(128)]
        public string Module { get; set; } = string.Empty; // e.g. Finance, Inventory, PLM

        [MaxLength(128)]
        public string? EntityName { get; set; } // e.g. Expense

        [MaxLength(128)]
        public string? EntityId { get; set; } // string to support GUID/int mixed

        [MaxLength(2048)]
        public string? Message { get; set; }

        public string? MetadataJson { get; set; }

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        [MaxLength(256)]
        public string? UserAgent { get; set; }

        public DateTimeOffset TimeStampUtc { get; set; } = DateTimeOffset.UtcNow;

        [NotMapped]
        public Dictionary<string, object?> Metadata
        {
            get => string.IsNullOrWhiteSpace(MetadataJson)
                ? new Dictionary<string, object?>()
                : (JsonSerializer.Deserialize<Dictionary<string, object?>>(MetadataJson) ?? new Dictionary<string, object?>());
            set => MetadataJson = JsonSerializer.Serialize(value ?? new Dictionary<string, object?>());
        }

    // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
