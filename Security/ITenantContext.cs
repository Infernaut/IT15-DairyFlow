using IT15_DairyFlow.Models;

namespace IT15_DairyFlow.Security
{
    public interface ITenantContext
    {
        int? CompanyId { get; }
        bool IsSuperAdmin { get; }
        string? UserId { get; }
        Task<ApplicationUser?> GetUserAsync(CancellationToken ct = default);
    }
}
