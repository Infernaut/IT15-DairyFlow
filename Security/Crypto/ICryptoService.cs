using System.Security.Cryptography;

namespace IT15_DairyFlow.Security.Crypto
{
    public interface ICryptoService
    {
        string EncryptToBase64(string plaintext);
        string DecryptFromBase64(string ciphertextBase64);

        /// <summary>
        /// Computes a deterministic lookup hash for equality searches (uses HMAC-SHA256 with an app secret).
        /// </summary>
        string ComputeLookupHash(string input);
    }
}
