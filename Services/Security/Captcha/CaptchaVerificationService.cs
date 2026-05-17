using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IT15_DairyFlow.Services.Security.Captcha;

public interface ICaptchaVerificationService
{
    Task<CaptchaVerificationResult> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default);
}

public sealed record CaptchaVerificationResult(bool Success, string? Error);

/// <summary>
/// Server-side verification for Google reCAPTCHA.
/// Supports reCAPTCHA v2 checkbox (response token) and can be extended for v3.
/// </summary>
public sealed class CaptchaVerificationService : ICaptchaVerificationService
{
    private readonly HttpClient _http;
    private readonly CaptchaSettings _settings;
    private readonly ILogger<CaptchaVerificationService> _logger;

    public CaptchaVerificationService(HttpClient http, IOptions<CaptchaSettings> settings, ILogger<CaptchaVerificationService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<CaptchaVerificationResult> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            return new CaptchaVerificationResult(true, null);
        }

        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            // Fail closed in production.
            return new CaptchaVerificationResult(false, "CAPTCHA is not configured.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return new CaptchaVerificationResult(false, "Please complete the CAPTCHA.");
        }

        // Google verify endpoint
        var payload = new Dictionary<string, string>
        {
            ["secret"] = _settings.SecretKey,
            ["response"] = token
        };
        if (!string.IsNullOrWhiteSpace(remoteIp))
            payload["remoteip"] = remoteIp;

        using var content = new FormUrlEncodedContent(payload);
        using var resp = await _http.PostAsync("https://www.google.com/recaptcha/api/siteverify", content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return new CaptchaVerificationResult(false, "CAPTCHA verification failed. Please try again.");
        }

        var dto = await resp.Content.ReadFromJsonAsync<GoogleRecaptchaVerifyResponse>(cancellationToken: ct);
        if (dto == null)
        {
            return new CaptchaVerificationResult(false, "CAPTCHA verification failed. Please try again.");
        }

        _logger.LogInformation(
            "reCAPTCHA verify result: success={Success}, score={Score}, action={Action}, hostname={Hostname}",
            dto.success,
            dto.score,
            dto.action,
            dto.hostname);

        if (!dto.success)
        {
            return new CaptchaVerificationResult(false, "CAPTCHA verification failed. Please try again.");
        }

        // v2 checkbox doesn't use score; dto.score will be null.
        if (_settings.MinimumScore.HasValue)
        {
            // If MinimumScore is configured, we expect a v3 score. Fail closed if score is missing.
            if (!dto.score.HasValue)
            {
                return new CaptchaVerificationResult(false, "CAPTCHA verification failed. Please try again.");
            }

            if (dto.score.Value < _settings.MinimumScore.Value)
            {
                return new CaptchaVerificationResult(false, "CAPTCHA verification failed. Please try again.");
            }
        }

        return new CaptchaVerificationResult(true, null);
    }

    private sealed class GoogleRecaptchaVerifyResponse
    {
        public bool success { get; set; }
        public decimal? score { get; set; }
        public string? action { get; set; }
        public string? hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? errorCodes { get; set; }
    }
}
