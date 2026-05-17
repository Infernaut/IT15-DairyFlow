using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Models.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Services.Security
{
    public sealed class PasswordHistoryService : IPasswordHistoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<ApplicationUser> _hasher;

        public PasswordHistoryService(ApplicationDbContext db, IPasswordHasher<ApplicationUser> hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        public async Task<bool> IsPasswordReusedAsync(string userId, string newPassword, int historyDepth, CancellationToken ct = default)
        {
            if (historyDepth <= 0) return true;

            var user = new ApplicationUser { Id = userId };
            var recent = await _db.PasswordHistories
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedUtc)
                .Take(historyDepth)
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var entry in recent)
            {
                var verify = _hasher.VerifyHashedPassword(user, entry.PasswordHash, newPassword);
                if (verify == PasswordVerificationResult.Success || verify == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task RecordPasswordHashAsync(string userId, string passwordHash, int historyDepth, CancellationToken ct = default)
        {
            _db.PasswordHistories.Add(new PasswordHistory
            {
                UserId = userId,
                PasswordHash = passwordHash,
                CreatedUtc = DateTimeOffset.UtcNow
            });

            await _db.SaveChangesAsync(ct);

            if (historyDepth > 0)
            {
                var toDelete = await _db.PasswordHistories
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.CreatedUtc)
                    .Skip(historyDepth)
                    .ToListAsync(ct);

                if (toDelete.Count > 0)
                {
                    _db.PasswordHistories.RemoveRange(toDelete);
                    await _db.SaveChangesAsync(ct);
                }
            }
        }
    }
}
