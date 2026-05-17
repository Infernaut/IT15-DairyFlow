using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models.Security
{
    public class PasswordHistory
    {
        [Key]
        public long PasswordHistoryId { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(2048)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
