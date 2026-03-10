using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Controllers
{
    /// <summary>
    /// API controller for the notification bell – returns JSON, no views.
    /// </summary>
    [Authorize]
    [Route("api/notifications")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// GET /api/notifications?take=20
        /// Returns the most recent notifications for the current user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int take = 20)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var notifications = await _db.Notification
                .Where(n => n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(Math.Clamp(take, 1, 50))
                .Select(n => new
                {
                    n.Id,
                    n.Message,
                    n.Type,
                    n.Icon,
                    n.IsRead,
                    n.CreatedAt,
                    ActorName = n.Actor != null ? n.Actor.UserName : null
                })
                .ToListAsync();

            return Ok(notifications);
        }

        /// <summary>
        /// GET /api/notifications/unread-count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var count = await _db.Notification
                .CountAsync(n => n.RecipientUserId == userId && !n.IsRead);

            return Ok(new { count });
        }

        /// <summary>
        /// POST /api/notifications/mark-read/5
        /// </summary>
        [HttpPost("mark-read/{id:int}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var notification = await _db.Notification
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        /// <summary>
        /// POST /api/notifications/mark-all-read
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _db.Notification
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return Ok(new { success = true });
        }
    }
}
