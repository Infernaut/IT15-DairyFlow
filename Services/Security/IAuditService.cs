namespace IT15_DairyFlow.Services.Security
{
    public interface IAuditService
    {
        Task LogAsync(
            string module,
            string actionType,
            string? message = null,
            string? entityName = null,
            string? entityId = null,
            Dictionary<string, object?>? metadata = null,
            int? companyIdOverride = null,
            string? userIdOverride = null,
            CancellationToken ct = default);
    }
}
