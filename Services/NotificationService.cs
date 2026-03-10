using IT15_DairyFlow.Data;
using IT15_DairyFlow.Hubs;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Services
{
    /// <summary>
    /// Centralised service for creating notifications.
    /// When an action occurs:
    ///   • The acting user gets a personal notification (regular user feed).
    ///   • All Admins in the same company also get a copy (admin feed).
    ///   • For super-admin–level events, all Superadmin users get a notification.
    /// </summary>
    public class NotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<DairyFlowHub> _hub;

        public NotificationService(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IHubContext<DairyFlowHub> hub)
        {
            _db = db;
            _userManager = userManager;
            _hub = hub;
        }

        // ── Company-scoped notification (regular + admin feeds) ──────────

        /// <summary>
        /// Creates a notification for the acting user AND for every Admin user
        /// in the same company (excluding duplicates if the actor is also Admin).
        /// </summary>
        public async Task NotifyCompanyActionAsync(
            string actorUserId,
            int companyId,
            string message,
            string type = "System",
            string icon = "bi-bell")
        {
            var recipients = new HashSet<string> { actorUserId };

            // Find all Admins in the same company
            var adminsInCompany = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in adminsInCompany.Where(a => a.CompanyID == companyId))
            {
                recipients.Add(admin.Id);
            }

            var notifications = recipients.Select(uid => new Notification
            {
                RecipientUserId = uid,
                ActorUserId = actorUserId,
                Message = message,
                Type = type,
                Icon = icon,
                IsRead = false,
                CreatedAt = DateTime.Now
            }).ToList();

            _db.Notification.AddRange(notifications);
            await _db.SaveChangesAsync();

            // Push real-time update to the company group
            await _hub.Clients.Group($"company-{companyId}")
                .SendAsync("NewNotification", new
                {
                    message,
                    type,
                    icon,
                    actorUserId,
                    timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
                });
        }

        // ── Super-admin notification ─────────────────────────────────────

        /// <summary>
        /// Creates a notification for ALL Superadmin users.
        /// </summary>
        public async Task NotifySuperAdminsAsync(
            string message,
            string type = "System",
            string icon = "bi-shield-check",
            string? actorUserId = null)
        {
            var superAdmins = await _userManager.GetUsersInRoleAsync("Superadmin");

            if (!superAdmins.Any()) return;

            var notifications = superAdmins.Select(sa => new Notification
            {
                RecipientUserId = sa.Id,
                ActorUserId = actorUserId,
                Message = message,
                Type = type,
                Icon = icon,
                IsRead = false,
                CreatedAt = DateTime.Now
            }).ToList();

            _db.Notification.AddRange(notifications);
            await _db.SaveChangesAsync();

            // Push to each superadmin via the superadmin group
            await _hub.Clients.Group("superadmin")
                .SendAsync("NewNotification", new
                {
                    message,
                    type,
                    icon,
                    actorUserId,
                    timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
                });
        }
    }
}
