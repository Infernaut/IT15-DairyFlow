using IT15_DairyFlow.Models;

namespace IT15_DairyFlow.Services.SystemLogging;

public interface ISystemLogService
{
    Task LogAsync(
        string component,
        string eventName,
        string level = "Information",
        string? message = null,
        int? companyId = null,
        string? userId = null,
        long? durationMs = null,
        string? correlationId = null,
        Dictionary<string, object?>? metadata = null,
        CancellationToken ct = default);
}
