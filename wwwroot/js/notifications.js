// ═══════════════════════════════════════════════════════════════════════════════
// DairyFlow Notification Bell — Fetches, renders, and manages notifications
// ═══════════════════════════════════════════════════════════════════════════════
(function () {
    'use strict';

    const bell = document.getElementById('notificationBellBtn');
    const dropdown = document.getElementById('notificationDropdown');
    const badge = document.getElementById('notificationBadge');
    const list = document.getElementById('notificationList');
    const markAllBtn = document.getElementById('markAllReadBtn');

    if (!bell || !dropdown) return; // not authenticated

    let isOpen = false;

    // ── Toggle dropdown ──────────────────────────────────────────────────

    bell.addEventListener('click', function (e) {
        e.stopPropagation();
        isOpen = !isOpen;
        dropdown.classList.toggle('show', isOpen);
        bell.setAttribute('aria-expanded', isOpen);
        if (isOpen) fetchNotifications();
    });

    // Close on outside click
    document.addEventListener('click', function (e) {
        if (isOpen && !dropdown.contains(e.target) && e.target !== bell) {
            isOpen = false;
            dropdown.classList.remove('show');
            bell.setAttribute('aria-expanded', 'false');
        }
    });

    // ── Fetch notifications from API ─────────────────────────────────────

    async function fetchNotifications() {
        try {
            const res = await fetch('/api/notifications?take=20', { credentials: 'same-origin' });
            if (!res.ok) return;
            const data = await res.json();
            renderNotifications(data);
        } catch (err) {
            console.warn('[Notifications] Fetch failed', err);
        }
    }

    async function fetchUnreadCount() {
        try {
            const res = await fetch('/api/notifications/unread-count', { credentials: 'same-origin' });
            if (!res.ok) return;
            const data = await res.json();
            updateBadge(data.count);
        } catch (err) {
            console.warn('[Notifications] Count fetch failed', err);
        }
    }

    // ── Render ───────────────────────────────────────────────────────────

    function renderNotifications(items) {
        if (!items || items.length === 0) {
            list.innerHTML = `
                <div class="text-center text-muted py-4" style="font-size:0.82rem;">
                    <i class="bi bi-bell-slash d-block mb-1" style="font-size:1.5rem;"></i>
                    No notifications yet
                </div>`;
            return;
        }

        let html = '';
        for (const n of items) {
            const readClass = n.isRead ? 'notification-read' : 'notification-unread';
            const timeAgo = relativeTime(n.createdAt);
            html += `
                <div class="notification-item ${readClass}" data-id="${n.id}" role="menuitem">
                    <div class="d-flex align-items-start gap-2">
                        <i class="bi ${n.icon || 'bi-bell'} notification-icon mt-1"></i>
                        <div class="flex-grow-1" style="min-width:0;">
                            <div class="notification-msg">${escapeHtml(n.message)}</div>
                            <div class="notification-meta">
                                ${n.actorName ? '<span class="me-2">' + escapeHtml(n.actorName) + '</span>' : ''}
                                <span>${timeAgo}</span>
                            </div>
                        </div>
                    </div>
                </div>`;
        }
        list.innerHTML = html;

        // Click to mark as read
        list.querySelectorAll('.notification-unread').forEach(function (el) {
            el.addEventListener('click', function () {
                markRead(parseInt(el.dataset.id));
                el.classList.remove('notification-unread');
                el.classList.add('notification-read');
            });
        });
    }

    // ── Mark read ────────────────────────────────────────────────────────

    async function markRead(id) {
        try {
            await fetch(`/api/notifications/mark-read/${id}`, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'RequestVerificationToken': getAntiForgeryToken() }
            });
            fetchUnreadCount();
        } catch (err) {
            console.warn('[Notifications] Mark read failed', err);
        }
    }

    markAllBtn?.addEventListener('click', async function (e) {
        e.stopPropagation();
        try {
            await fetch('/api/notifications/mark-all-read', {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'RequestVerificationToken': getAntiForgeryToken() }
            });
            list.querySelectorAll('.notification-unread').forEach(function (el) {
                el.classList.remove('notification-unread');
                el.classList.add('notification-read');
            });
            updateBadge(0);
        } catch (err) {
            console.warn('[Notifications] Mark all read failed', err);
        }
    });

    // ── Badge ────────────────────────────────────────────────────────────

    function updateBadge(count) {
        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.classList.remove('d-none');
        } else {
            badge.classList.add('d-none');
        }
    }

    // ── Real-time SignalR integration ─────────────────────────────────────

    document.addEventListener('dairyflow:new-notification', function () {
        fetchUnreadCount();
        if (isOpen) fetchNotifications();
    });

    // ── Helpers ──────────────────────────────────────────────────────────

    function relativeTime(dateStr) {
        const now = new Date();
        const date = new Date(dateStr);
        const diff = Math.floor((now - date) / 1000);
        if (diff < 60) return 'Just now';
        if (diff < 3600) return Math.floor(diff / 60) + 'm ago';
        if (diff < 86400) return Math.floor(diff / 3600) + 'h ago';
        if (diff < 604800) return Math.floor(diff / 86400) + 'd ago';
        return date.toLocaleDateString();
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    // ── Initial load ─────────────────────────────────────────────────────

    fetchUnreadCount();
    // Poll every 60 seconds as a fallback to SignalR
    setInterval(fetchUnreadCount, 60000);
})();
