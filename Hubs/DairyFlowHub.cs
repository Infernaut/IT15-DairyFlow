using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace IT15_DairyFlow.Hubs
{
    /// <summary>
    /// Central SignalR hub for real-time notifications across all DairyFlow modules.
    /// Clients are grouped by CompanyID so each tenant only sees their own events.
    /// Superadmin users are also added to the "superadmin" group.
    /// </summary>
    [Authorize]
    public class DairyFlowHub : Hub
    {
        private readonly ILogger<DairyFlowHub> _logger;

        public DairyFlowHub(ILogger<DairyFlowHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// On connect, add the user to their company group (passed as query param).
        /// Also adds Superadmin users to the "superadmin" group.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var companyId = Context.GetHttpContext()?.Request.Query["companyId"].ToString();
            if (!string.IsNullOrEmpty(companyId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"company-{companyId}");
                _logger.LogInformation("SignalR: {User} joined company-{CompanyId}", Context.User?.Identity?.Name, companyId);
            }

            // Add Superadmin users to the superadmin group for platform-level notifications
            if (Context.User?.IsInRole("Superadmin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "superadmin");
                _logger.LogInformation("SignalR: {User} joined superadmin group", Context.User?.Identity?.Name);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var companyId = Context.GetHttpContext()?.Request.Query["companyId"].ToString();
            if (!string.IsNullOrEmpty(companyId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company-{companyId}");
            }

            if (Context.User?.IsInRole("Superadmin") == true)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "superadmin");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// Helper service for sending SignalR notifications from controllers.
    /// Inject IHubContext&lt;DairyFlowHub&gt; and use these extension methods.
    /// </summary>
    public static class DairyFlowHubExtensions
    {
        // ── Production Notifications ──────────────────────────────────────

        public static async Task NotifyBatchCreated(this IHubContext<DairyFlowHub> hub,
            int companyId, string batchCode, string productName, string createdBy)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("BatchCreated", new
            {
                batchCode,
                productName,
                createdBy,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        public static async Task NotifyBatchCompleted(this IHubContext<DairyFlowHub> hub,
            int companyId, string batchCode, string productName)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("BatchCompleted", new
            {
                batchCode,
                productName,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        // ── Inventory Notifications ───────────────────────────────────────

        public static async Task NotifyLowStock(this IHubContext<DairyFlowHub> hub,
            int companyId, string itemName, int currentQty, int threshold)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("LowStockAlert", new
            {
                itemName,
                currentQty,
                threshold,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        public static async Task NotifyInventoryUpdated(this IHubContext<DairyFlowHub> hub,
            int companyId, string action, string itemName, string updatedBy)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("InventoryUpdated", new
            {
                action,
                itemName,
                updatedBy,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        // ── Quality Notifications ─────────────────────────────────────────

        public static async Task NotifyInspectionCompleted(this IHubContext<DairyFlowHub> hub,
            int companyId, string batchCode, string result, string inspector)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("InspectionCompleted", new
            {
                batchCode,
                result,
                inspector,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        public static async Task NotifyBatchOnHold(this IHubContext<DairyFlowHub> hub,
            int companyId, string batchCode, string reason)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("BatchOnHold", new
            {
                batchCode,
                reason,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        // ── Finance Notifications ─────────────────────────────────────────

        public static async Task NotifyExpenseCreated(this IHubContext<DairyFlowHub> hub,
            int companyId, string category, decimal amount, string createdBy)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("ExpenseCreated", new
            {
                category,
                amount,
                createdBy,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        public static async Task NotifyInvoicePaid(this IHubContext<DairyFlowHub> hub,
            int companyId, decimal amount)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("InvoicePaid", new
            {
                amount,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        // ── Sales Notifications ───────────────────────────────────────────

        public static async Task NotifySaleCreated(this IHubContext<DairyFlowHub> hub,
            int companyId, string invoiceNumber, string productName, string createdBy)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("SaleCreated", new
            {
                invoiceNumber,
                productName,
                createdBy,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        public static async Task NotifyPaymentProcessed(this IHubContext<DairyFlowHub> hub,
            int companyId, string invoiceNumber, decimal amount, string paymentMethod)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("PaymentProcessed", new
            {
                invoiceNumber,
                amount,
                paymentMethod,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }

        // ── Dashboard Refresh ─────────────────────────────────────────────

        /// <summary>
        /// Generic refresh signal — tells dashboard clients to re-fetch KPI data.
        /// </summary>
        public static async Task NotifyDashboardRefresh(this IHubContext<DairyFlowHub> hub,
            int companyId, string module)
        {
            await hub.Clients.Group($"company-{companyId}").SendAsync("DashboardRefresh", new
            {
                module,
                timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm")
            });
        }
    }
}
