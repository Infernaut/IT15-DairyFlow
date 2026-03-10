// ═══════════════════════════════════════════════════════════════════════════════
// DairyFlow SignalR Client — Real-Time Notifications
// ═══════════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    // Only initialize for authenticated users with a companyId
    const companyId = document.documentElement.getAttribute('data-company-id');
    if (!companyId || typeof signalR === 'undefined') return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`/hubs/dairyflow?companyId=${companyId}`)
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    // ── Connection lifecycle ──────────────────────────────────────────────

    async function startConnection() {
        try {
            await connection.start();
            console.log('[SignalR] Connected to DairyFlow hub');
        } catch (err) {
            console.warn('[SignalR] Connection failed, retrying in 5s...', err);
            setTimeout(startConnection, 5000);
        }
    }

    connection.onreconnecting(() => {
        console.log('[SignalR] Reconnecting...');
    });

    connection.onreconnected(() => {
        console.log('[SignalR] Reconnected');
        showNotification('info', 'Real-time connection restored');
    });

    connection.onclose(() => {
        console.log('[SignalR] Disconnected, attempting restart...');
        setTimeout(startConnection, 5000);
    });

    // ── Notification display helper ───────────────────────────────────────

    function showNotification(type, message) {
        if (typeof showGlobalToast === 'function') {
            showGlobalToast(type === 'info' || type === 'success' ? 'success' : 'error', message);
        }
    }

    // ── Production Events ────────────────────────────────────────────────

    connection.on('BatchCreated', function (data) {
        showNotification('info',
            `New batch ${data.batchCode} (${data.productName}) created by ${data.createdBy}`);
        dispatchRefresh('production');
    });

    connection.on('BatchCompleted', function (data) {
        showNotification('success',
            `Batch ${data.batchCode} (${data.productName}) has been completed`);
        dispatchRefresh('production');
    });

    // ── Inventory Events ─────────────────────────────────────────────────

    connection.on('LowStockAlert', function (data) {
        showNotification('warning',
            `Low stock alert: ${data.itemName} — only ${data.currentQty} remaining`);
        dispatchRefresh('inventory');
    });

    connection.on('InventoryUpdated', function (data) {
        showNotification('info',
            `Inventory ${data.action}: ${data.itemName} by ${data.updatedBy}`);
        dispatchRefresh('inventory');
    });

    // ── Quality Events ───────────────────────────────────────────────────

    connection.on('InspectionCompleted', function (data) {
        showNotification(data.result === 'Passed' ? 'success' : 'warning',
            `Inspection ${data.result} for batch ${data.batchCode} by ${data.inspector}`);
        dispatchRefresh('quality');
    });

    connection.on('BatchOnHold', function (data) {
        showNotification('warning',
            `Batch ${data.batchCode} placed on hold: ${data.reason}`);
        dispatchRefresh('quality');
    });

    // ── Finance Events ───────────────────────────────────────────────────

    connection.on('ExpenseCreated', function (data) {
        showNotification('info',
            `New ${data.category} expense: ₱${parseFloat(data.amount).toLocaleString()} by ${data.createdBy}`);
        dispatchRefresh('finance');
    });

    connection.on('InvoicePaid', function (data) {
        showNotification('success',
            `Invoice paid: ₱${parseFloat(data.amount).toLocaleString()}`);
        dispatchRefresh('finance');
    });

    // ── Dashboard Refresh ────────────────────────────────────────────────

    connection.on('DashboardRefresh', function (data) {
        dispatchRefresh(data.module);
    });

    // ── Notification Bell Events ─────────────────────────────────────────

    connection.on('NewNotification', function (data) {
        // Dispatch a custom event so the notification bell JS can react
        document.dispatchEvent(new CustomEvent('dairyflow:new-notification', {
            detail: data
        }));
    });

    /**
     * Dispatch a custom event so page-specific scripts can react to real-time updates.
     * Usage in page scripts:
     *   document.addEventListener('dairyflow:refresh', e => {
     *       if (e.detail.module === 'production') reloadTable();
     *   });
     */
    function dispatchRefresh(module) {
        document.dispatchEvent(new CustomEvent('dairyflow:refresh', {
            detail: { module: module }
        }));
    }

    // Expose connection for page-specific scripts if needed
    window.dairyFlowConnection = connection;

    // Start the connection
    startConnection();
})();
