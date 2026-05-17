using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace IT15_DairyFlow.Security.Crypto
{
    public sealed class CryptoService : ICryptoService
    {
        private readonly byte[] _encKey;
        private readonly byte[] _hmacKey;

        public CryptoService(IOptions<CryptoSettings> options)
        {
            var s = options.Value;
            try
            {
                _encKey = Convert.FromBase64String(s.EncryptionKeyBase64);
                _hmacKey = Convert.FromBase64String(s.LookupKeyBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "Crypto keys are not configured correctly. Please set Crypto:EncryptionKeyBase64 and Crypto:LookupKeyBase64 to valid Base64 strings (32 bytes for encryption key).",
                    ex);
            }

            if (_encKey.Length != 32)
                throw new InvalidOperationException("Crypto:EncryptionKeyBase64 must be 32 bytes (base64-encoded).");
            if (_hmacKey.Length < 32)
                throw new InvalidOperationException("Crypto:LookupKeyBase64 must be at least 32 bytes (base64-encoded).");
        }

        public string EncryptToBase64(string plaintext)
        {
            if (plaintext == null) return string.Empty;

            // AES-GCM: output = nonce(12) + tag(16) + ciphertext
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(_encKey, 16);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            var combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        public string DecryptFromBase64(string ciphertextBase64)
        {
            if (string.IsNullOrWhiteSpace(ciphertextBase64)) return string.Empty;

            var combined = Convert.FromBase64String(ciphertextBase64);
            if (combined.Length < 12 + 16) throw new CryptographicException("Ciphertext too short.");

            var nonce = combined.AsSpan(0, 12).ToArray();
            var tag = combined.AsSpan(12, 16).ToArray();
            var cipher = combined.AsSpan(28).ToArray();

            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(_encKey, 16);
            aes.Decrypt(nonce, cipher, tag, plain);

            return Encoding.UTF8.GetString(plain);
        }

        public string ComputeLookupHash(string input)
        {
            input ??= string.Empty;
            var normalized = input.Trim().ToUpperInvariant();
            var bytes = Encoding.UTF8.GetBytes(normalized);

            using var h = new HMACSHA256(_hmacKey);
            var hash = h.ComputeHash(bytes);
            return Convert.ToHexString(hash); // deterministic uppercase hex
        }
    }
}
