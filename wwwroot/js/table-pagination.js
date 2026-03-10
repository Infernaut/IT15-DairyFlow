/**
 * DairyFlow — Client-side table pagination
 * Auto-applies to every <table> that has a <tbody> with data rows.
 * Tables can opt-out by adding the class "no-pagination".
 * Set data-page-size="N" on the table to override the default rows per page.
 */
(function () {
    'use strict';

    const DEFAULT_PAGE_SIZE = 10;

    function paginateTable(table) {
        if (table.classList.contains('no-pagination') || table.dataset.paginated === 'true') return;

        const tbody = table.querySelector('tbody');
        if (!tbody) return;

        const rows = Array.from(tbody.querySelectorAll('tr'));
        if (rows.length <= DEFAULT_PAGE_SIZE) return; // No need to paginate small tables

        const pageSize = parseInt(table.dataset.pageSize, 10) || DEFAULT_PAGE_SIZE;
        let currentPage = 1;
        const totalPages = Math.ceil(rows.length / pageSize);

        // Mark table as paginated
        table.dataset.paginated = 'true';

        // Create pagination wrapper
        const wrapper = document.createElement('div');
        wrapper.className = 'df-pagination-wrapper d-flex flex-wrap justify-content-between align-items-center px-4 py-3';
        wrapper.setAttribute('role', 'navigation');
        wrapper.setAttribute('aria-label', 'Table pagination');

        // Info text
        const info = document.createElement('span');
        info.className = 'df-pagination-info text-muted small';
        info.setAttribute('aria-live', 'polite');

        // Page controls container
        const controls = document.createElement('nav');
        controls.setAttribute('aria-label', 'Page navigation');
        const ul = document.createElement('ul');
        ul.className = 'pagination pagination-sm mb-0 gap-1';

        controls.appendChild(ul);
        wrapper.appendChild(info);
        wrapper.appendChild(controls);

        // Insert pagination after the table's parent (table-responsive div or table itself)
        const insertTarget = table.closest('.table-responsive') || table.closest('.card-body') || table.parentElement;
        if (insertTarget) {
            insertTarget.appendChild(wrapper);
        } else {
            table.parentNode.insertBefore(wrapper, table.nextSibling);
        }

        function render() {
            const start = (currentPage - 1) * pageSize;
            const end = start + pageSize;

            rows.forEach((row, i) => {
                row.style.display = (i >= start && i < end) ? '' : 'none';
            });

            const showStart = rows.length === 0 ? 0 : start + 1;
            const showEnd = Math.min(end, rows.length);
            info.textContent = `Showing ${showStart} to ${showEnd} of ${rows.length} entries`;

            renderButtons();
        }

        function renderButtons() {
            ul.innerHTML = '';

            // Previous button
            const prevLi = document.createElement('li');
            prevLi.className = 'page-item' + (currentPage === 1 ? ' disabled' : '');
            const prevBtn = document.createElement('button');
            prevBtn.className = 'page-link';
            prevBtn.innerHTML = '<i class="bi bi-chevron-left"></i>';
            prevBtn.setAttribute('aria-label', 'Previous page');
            prevBtn.disabled = currentPage === 1;
            prevBtn.addEventListener('click', () => { if (currentPage > 1) { currentPage--; render(); } });
            prevLi.appendChild(prevBtn);
            ul.appendChild(prevLi);

            // Page number buttons (show max 5 around current)
            const maxVisible = 5;
            let startPage = Math.max(1, currentPage - Math.floor(maxVisible / 2));
            let endPage = Math.min(totalPages, startPage + maxVisible - 1);
            if (endPage - startPage < maxVisible - 1) {
                startPage = Math.max(1, endPage - maxVisible + 1);
            }

            if (startPage > 1) {
                ul.appendChild(createPageBtn(1));
                if (startPage > 2) {
                    const dots = document.createElement('li');
                    dots.className = 'page-item disabled';
                    dots.innerHTML = '<span class="page-link">&hellip;</span>';
                    ul.appendChild(dots);
                }
            }

            for (let p = startPage; p <= endPage; p++) {
                ul.appendChild(createPageBtn(p));
            }

            if (endPage < totalPages) {
                if (endPage < totalPages - 1) {
                    const dots = document.createElement('li');
                    dots.className = 'page-item disabled';
                    dots.innerHTML = '<span class="page-link">&hellip;</span>';
                    ul.appendChild(dots);
                }
                ul.appendChild(createPageBtn(totalPages));
            }

            // Next button
            const nextLi = document.createElement('li');
            nextLi.className = 'page-item' + (currentPage === totalPages ? ' disabled' : '');
            const nextBtn = document.createElement('button');
            nextBtn.className = 'page-link';
            nextBtn.innerHTML = '<i class="bi bi-chevron-right"></i>';
            nextBtn.setAttribute('aria-label', 'Next page');
            nextBtn.disabled = currentPage === totalPages;
            nextBtn.addEventListener('click', () => { if (currentPage < totalPages) { currentPage++; render(); } });
            nextLi.appendChild(nextBtn);
            ul.appendChild(nextLi);
        }

        function createPageBtn(page) {
            const li = document.createElement('li');
            li.className = 'page-item' + (page === currentPage ? ' active' : '');
            const btn = document.createElement('button');
            btn.className = 'page-link';
            btn.textContent = page;
            btn.setAttribute('aria-label', `Page ${page}`);
            if (page === currentPage) btn.setAttribute('aria-current', 'page');
            btn.addEventListener('click', () => { currentPage = page; render(); });
            li.appendChild(btn);
            return li;
        }

        render();
    }

    // Initialize on DOM ready
    function init() {
        document.querySelectorAll('table').forEach(paginateTable);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose for dynamic tables
    window.DairyFlowPagination = { apply: paginateTable, init: init };
})();
