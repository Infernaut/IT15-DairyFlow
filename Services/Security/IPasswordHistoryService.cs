namespace IT15_DairyFlow.Services.Security
{
    public interface IPasswordHistoryService
    {
        /// <summary>
        /// True if the password can be used (i.e., it is NOT equal to any of the last N password hashes).
        /// </summary>
        Task<bool> IsPasswordReusedAsync(string userId, string newPassword, int historyDepth, CancellationToken ct = default);

        Task RecordPasswordHashAsync(string userId, string passwordHash, int historyDepth, CancellationToken ct = default);
    }
}
