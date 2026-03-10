using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The user who should see this notification.
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string RecipientUserId { get; set; } = string.Empty;

        /// <summary>
        /// The user who performed the action (null for system-generated).
        /// </summary>
        [MaxLength(450)]
        public string? ActorUserId { get; set; }

        /// <summary>
        /// Human-readable notification message.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Category: Admin, Production, Inventory, Quality, Finance, System
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "System";

        /// <summary>
        /// Bootstrap icon class (e.g. bi-gear, bi-box-seam).
        /// </summary>
        [MaxLength(50)]
        public string Icon { get; set; } = "bi-bell";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("RecipientUserId")]
        public virtual ApplicationUser Recipient { get; set; } = null!;

        [ForeignKey("ActorUserId")]
        public virtual ApplicationUser? Actor { get; set; }
    }
}
