using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    [Table("Equipment")]
    public class Equipment
    {
        [Key]
        public int EquipmentID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [Required]
        [MaxLength(200)]
        public string EquipmentName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EquipmentType { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        public DateTime? LastMaintenanceDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        public virtual ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();
    }
}
