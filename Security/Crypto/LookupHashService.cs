using System.Security.Cryptography;
using System.Text;

namespace IT15_DairyFlow.Security.Crypto;

public interface ILookupHashService
{
    /// <summary>
    /// Computes a deterministic, keyed lookup hash suitable for equality search.
    /// Output is Base64 (URL-safe enough for storage; not intended for display).
    /// </summary>
    string Compute(string input);
}

public sealed class LookupHashService : ILookupHashService
{
    private readonly CryptoSettings _settings;

    public LookupHashService(Microsoft.Extensions.Options.IOptions<CryptoSettings> settings)
    {
        _settings = settings.Value;
    }

    public string Compute(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

    // Reuse the existing lookup key from CryptoSettings.
    var key = Convert.FromBase64String(_settings.LookupKeyBase64);
        using var hmac = new HMACSHA256(key);
        var bytes = Encoding.UTF8.GetBytes(input.Trim().ToLowerInvariant());
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
