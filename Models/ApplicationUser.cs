using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT15_DairyFlow.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? CompanyID { get; set; }

        /// <summary>
        /// Deterministic hash for finding users by email without storing email in plaintext.
        /// Computed using HMAC-SHA256 over normalized email.
        /// </summary>
        [PersonalData]
        [MaxLength(64)]
        public string? EmailLookupHash { get; set; }

        /// <summary>
        /// Encrypted email for display/recovery flows. Stored as base64(AES-GCM(nonce+tag+ciphertext)).
        /// </summary>
        [PersonalData]
        public string? EmailEncrypted { get; set; }

        [ForeignKey("CompanyID")]
        public virtual Company? Company { get; set; }
    }
}
