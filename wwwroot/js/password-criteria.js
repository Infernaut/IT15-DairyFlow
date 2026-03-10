/**
 * DairyFlow — Real-time password criteria checker
 * Attach to any password field by calling:
 *   DairyFlowPassword.attach(inputSelector, containerSelector)
 * Or auto-attaches to inputs with data-password-criteria="true"
 *
 * ASP.NET Core Identity defaults:
 *   - At least 6 characters
 *   - At least 1 uppercase letter
 *   - At least 1 lowercase letter
 *   - At least 1 digit
 *   - At least 1 non-alphanumeric character (!@@#$%^&* etc.)
 */
(function () {
    'use strict';

    const criteria = [
        { id: 'length', label: 'At least 6 characters', test: v => v.length >= 6 },
        { id: 'upper', label: 'At least 1 uppercase letter (A-Z)', test: v => /[A-Z]/.test(v) },
        { id: 'lower', label: 'At least 1 lowercase letter (a-z)', test: v => /[a-z]/.test(v) },
        { id: 'digit', label: 'At least 1 digit (0-9)', test: v => /\d/.test(v) },
        { id: 'special', label: 'At least 1 special character (!@@#$%^&*)', test: v => /[^A-Za-z0-9]/.test(v) }
    ];

    function createCriteriaUI() {
        const container = document.createElement('div');
        container.className = 'df-password-criteria mt-2 p-3 rounded-3 border';
        container.style.background = '#f8fafc';
        container.setAttribute('role', 'status');
        container.setAttribute('aria-live', 'polite');
        container.setAttribute('aria-label', 'Password requirements');

        const title = document.createElement('div');
        title.className = 'small fw-semibold text-muted mb-2';
        title.innerHTML = '<i class="bi bi-shield-lock me-1"></i> Password must contain:';
        container.appendChild(title);

        const list = document.createElement('ul');
        list.className = 'list-unstyled mb-0';
        list.style.fontSize = '0.82rem';

        criteria.forEach(c => {
            const li = document.createElement('li');
            li.className = 'df-pw-rule d-flex align-items-center mb-1';
            li.dataset.rule = c.id;
            li.innerHTML = `<i class="bi bi-circle me-2 text-muted" style="font-size: 0.5rem;"></i><span class="text-muted">${c.label}</span>`;
            list.appendChild(li);
        });

        container.appendChild(list);
        return container;
    }

    function updateCriteria(container, value) {
        let allMet = true;
        criteria.forEach(c => {
            const li = container.querySelector(`[data-rule="${c.id}"]`);
            if (!li) return;
            const passed = c.test(value);
            if (!passed) allMet = false;
            const icon = li.querySelector('i');
            const span = li.querySelector('span');
            if (passed) {
                icon.className = 'bi bi-check-circle-fill me-2 text-success';
                icon.style.fontSize = '';
                span.className = 'text-success';
            } else {
                icon.className = 'bi bi-circle me-2 text-muted';
                icon.style.fontSize = '0.5rem';
                span.className = 'text-muted';
            }
        });

        // Update border color based on status
        if (value.length === 0) {
            container.style.borderColor = '#dee2e6';
        } else if (allMet) {
            container.style.borderColor = '#198754';
        } else {
            container.style.borderColor = '#dc3545';
        }
    }

    function attach(input) {
        if (typeof input === 'string') {
            input = document.querySelector(input);
        }
        if (!input || input.dataset.criteriaAttached === 'true') return;
        input.dataset.criteriaAttached = 'true';

        const uiContainer = createCriteriaUI();

        // Insert after the password field's parent (.form-floating or .mb-3)
        const formGroup = input.closest('.form-floating') || input.closest('.mb-3') || input.parentElement;
        const validationSpan = formGroup.nextElementSibling;
        if (validationSpan && validationSpan.classList.contains('text-danger')) {
            validationSpan.after(uiContainer);
        } else {
            formGroup.after(uiContainer);
        }

        input.addEventListener('input', function () {
            updateCriteria(uiContainer, this.value);
        });

        // Initial state
        updateCriteria(uiContainer, input.value || '');
    }

    // Auto-attach to inputs with data attribute
    function init() {
        document.querySelectorAll('[data-password-criteria="true"]').forEach(attach);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.DairyFlowPassword = { attach: attach, init: init };
})();
