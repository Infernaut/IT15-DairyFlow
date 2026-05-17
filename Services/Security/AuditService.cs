using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Security;
using Microsoft.AspNetCore.Identity;

namespace IT15_DairyFlow.Services.Security
{
    public sealed class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;
        private readonly ITenantContext _tenant;
        private readonly IHttpContextAccessor _http;
        private readonly UserManager<ApplicationUser> _users;

        public AuditService(ApplicationDbContext db, ITenantContext tenant, IHttpContextAccessor http, UserManager<ApplicationUser> users)
        {
            _db = db;
            _tenant = tenant;
            _http = http;
            _users = users;
        }

        public async Task LogAsync(
            string module,
            string actionType,
            string? message = null,
            string? entityName = null,
            string? entityId = null,
            Dictionary<string, object?>? metadata = null,
            int? companyIdOverride = null,
            string? userIdOverride = null,
            CancellationToken ct = default)
        {
            var http = _http.HttpContext;
            var userId = userIdOverride ?? _tenant.UserId;

            int? companyId = companyIdOverride;
            if (!companyId.HasValue && !string.IsNullOrWhiteSpace(userId) && !_tenant.IsSuperAdmin)
            {
                var u = await _users.FindByIdAsync(userId);
                companyId = u?.CompanyID;
            }

            // For Superadmin, allow companyIdOverride for system monitoring; otherwise we require companyId.
            if (!companyId.HasValue)
            {
                // If there's no company context, skip DB insert (keeps audit insert from breaking auth flows).
                return;
            }

            if (string.IsNullOrWhiteSpace(userId)) userId = string.Empty;

            var log = new AuditLog
            {
                CompanyID = companyId.Value,
                UserID = userId,
                Module = module,
                ActionType = actionType,
                Message = message,
                EntityName = entityName,
                EntityId = entityId,
                Metadata = metadata ?? new Dictionary<string, object?>(),
                IpAddress = http?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = http?.Request?.Headers.UserAgent.ToString(),
                TimeStampUtc = DateTimeOffset.UtcNow
            };

            _db.AuditLog.Add(log);
            await _db.SaveChangesAsync(ct);
        }
    }
}
