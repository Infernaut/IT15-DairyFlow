using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IT15_DairyFlow.Security
{
    /// <summary>
    /// Provides the authenticated user's company/tenant. In this codebase, tenant = CompanyID from ApplicationUser.
    /// </summary>
    public sealed class TenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _http;
        private readonly UserManager<ApplicationUser> _users;

        private ApplicationUser? _cachedUser;

        public TenantContext(IHttpContextAccessor http, UserManager<ApplicationUser> users)
        {
            _http = http;
            _users = users;
        }

        public string? UserId => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public bool IsSuperAdmin => _http.HttpContext?.User?.IsInRole(AppRoles.Superadmin) == true;

        public int? CompanyId
        {
            get
            {
                if (IsSuperAdmin) return null;
                return _cachedUser?.CompanyID;
            }
        }

        public async Task<ApplicationUser?> GetUserAsync(CancellationToken ct = default)
        {
            if (_cachedUser != null) return _cachedUser;
            var principal = _http.HttpContext?.User;
            if (principal == null || principal.Identity?.IsAuthenticated != true) return null;
            _cachedUser = await _users.GetUserAsync(principal);
            return _cachedUser;
        }
    }
}
