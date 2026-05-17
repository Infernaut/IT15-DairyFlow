using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Services.SystemLogging;

/// <summary>
/// Writes operational/technical events into the database.
/// Do not use this for user/business actions; use AuditService for that.
/// </summary>
public sealed class SystemLogService : ISystemLogService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public SystemLogService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(
        string component,
        string eventName,
        string level = "Information",
        string? message = null,
        int? companyId = null,
        string? userId = null,
        long? durationMs = null,
        string? correlationId = null,
        Dictionary<string, object?>? metadata = null,
        CancellationToken ct = default)
    {
        // Avoid breaking user flows if logging fails.
        try
        {
            var http = _http.HttpContext;
            var corr = correlationId;
            if (string.IsNullOrWhiteSpace(corr))
            {
                // Prefer standard trace identifier.
                corr = http?.TraceIdentifier;
            }

            var log = new SystemLog
            {
                TimeStampUtc = DateTimeOffset.UtcNow,
                Level = string.IsNullOrWhiteSpace(level) ? "Information" : level,
                Component = component ?? string.Empty,
                EventName = eventName ?? string.Empty,
                Message = message,
                CompanyId = companyId,
                UserId = userId,
                DurationMs = durationMs,
                CorrelationId = corr,
                IpAddress = http?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = http?.Request?.Headers.UserAgent.ToString(),
                Metadata = metadata ?? new Dictionary<string, object?>()
            };

            _db.SystemLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // Swallow errors: system logging must not take down the app.
            // (If you want, we can also fall back to ILogger here.)
        }
    }
}
