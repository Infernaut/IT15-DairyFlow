using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? CompanyID { get; set; }

        [ForeignKey("CompanyID")]
        public virtual Company? Company { get; set; }
    }
}
