namespace IT15_DairyFlow.Services.Security.Captcha;

public sealed class CaptchaSettings
{
    public const string SectionName = "Captcha";

    /// <summary>
    /// Google reCAPTCHA site key (public).
    /// </summary>
    public string SiteKey { get; set; } = string.Empty;

    /// <summary>
    /// Google reCAPTCHA secret key (server-side).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Minimum score when using reCAPTCHA v3 (not used for v2 checkbox).
    /// </summary>
    public decimal? MinimumScore { get; set; }

    /// <summary>
    /// Enables CAPTCHA globally. Keep true in Production.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
