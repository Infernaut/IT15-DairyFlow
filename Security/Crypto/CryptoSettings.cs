namespace IT15_DairyFlow.Security.Crypto
{
    public sealed class CryptoSettings
    {
        public const string SectionName = "Crypto";

        /// <summary>
        /// Base64 encoded 32-byte key for AES-GCM.
        /// </summary>
        public string EncryptionKeyBase64 { get; set; } = string.Empty;

        /// <summary>
        /// Base64 encoded key (>= 32 bytes) for HMAC-SHA256 lookup hashes.
        /// </summary>
        public string LookupKeyBase64 { get; set; } = string.Empty;

        public int PasswordReuseHistoryDepth { get; set; } = 5;
    }
}
