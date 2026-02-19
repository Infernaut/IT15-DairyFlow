using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }

        [Required]
        public int CompanyID { get; set; }

        [MaxLength(256)]
        public string? Name { get; set; }

        public string? ContactInfo { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        // Navigation properties
        [ForeignKey("CompanyID")]
        public virtual Company Company { get; set; } = null!;

        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual ICollection<RawMaterial> RawMaterials { get; set; } = new List<RawMaterial>();
    }
}
