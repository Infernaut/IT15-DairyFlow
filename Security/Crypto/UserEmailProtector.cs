using IT15_DairyFlow.Models;

namespace IT15_DairyFlow.Security.Crypto
{
    public static class UserEmailProtector
    {
        public static void ProtectEmail(ApplicationUser user, ICryptoService crypto, string email)
        {
            var normalized = (email ?? string.Empty).Trim();

            // Identity internal fields (kept for compatibility and normalization)
            user.Email = normalized;
            user.NormalizedEmail = normalized.ToUpperInvariant();

            // Protected storage
            user.EmailLookupHash = crypto.ComputeLookupHash(normalized);
            user.EmailEncrypted = crypto.EncryptToBase64(normalized);
        }
    }
}
