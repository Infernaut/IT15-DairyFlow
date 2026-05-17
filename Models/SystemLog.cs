using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace IT15_DairyFlow.Models;

/// <summary>
/// System/operational logs for platform health and technical performance.
/// This is separate from <see cref="AuditLog"/>, which captures user/business actions.
/// </summary>
public sealed class SystemLog
{
    [Key]
    public long SystemLogId { get; set; }

    /// <summary>
    /// UTC timestamp for when the event occurred.
    /// </summary>
    public DateTimeOffset TimeStampUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Severity level (Trace/Debug/Information/Warning/Error/Critical).
    /// </summary>
    [MaxLength(32)]
    public string Level { get; set; } = "Information";

    /// <summary>
    /// Component/module emitting the event (e.g., Identity, EF, BackgroundJobs, Captcha).
    /// </summary>
    [MaxLength(128)]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// A short, stable event name (e.g., RequestCompleted, DbSlowQuery, EmailSendFailed).
    /// </summary>
    [MaxLength(128)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message. Avoid secrets.
    /// </summary>
    [MaxLength(2048)]
    public string? Message { get; set; }

    /// <summary>
    /// Optional company context. This supports cross-tenant transparency
    /// while still allowing global/system-wide events (null).
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>
    /// Optional user context (e.g., for auth failures). Keep null for pure operational telemetry.
    /// </summary>
    [MaxLength(450)]
    public string? UserId { get; set; }

    /// <summary>
    /// Correlation ID for tracing across requests.
    /// </summary>
    [MaxLength(64)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Basic request context.
    /// </summary>
    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(256)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Duration/latency in milliseconds (if applicable).
    /// </summary>
    public long? DurationMs { get; set; }

    public string? MetadataJson { get; set; }

    [NotMapped]
    public Dictionary<string, object?> Metadata
    {
        get => string.IsNullOrWhiteSpace(MetadataJson)
            ? new Dictionary<string, object?>()
            : (JsonSerializer.Deserialize<Dictionary<string, object?>>(MetadataJson) ?? new Dictionary<string, object?>());
        set => MetadataJson = JsonSerializer.Serialize(value ?? new Dictionary<string, object?>());
    }
}
