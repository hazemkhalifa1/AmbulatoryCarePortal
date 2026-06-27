// CBAHI Portal — Modern UI Interactions
document.addEventListener('DOMContentLoaded', () => {
    initTooltips();
    initConfirmation();
    initSidebar();
    initActiveNav();
    initNotificationPolling();
    initPasswordToggle();
    initTableSearch();
    initLoadingButtons();
    initLanguageToggle();
    initSidebarKeyboard();
    initPasswordStrength();
});

// ============================================================
// TOOLTIPS
// ============================================================
function initTooltips() {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => new bootstrap.Tooltip(el));
}

// ============================================================
// CONFIRMATION DIALOGS
// ============================================================
function initConfirmation() {
    document.addEventListener('click', e => {
        const target = e.target.closest('[data-confirm]');
        if (!target) return;
        e.preventDefault();
        if (confirm(target.getAttribute('data-confirm'))) {
            const form = target.closest('form');
            if (form) form.submit();
            else if (target.tagName === 'A') window.location.href = target.href;
        }
    });
}

// ============================================================
// SIDEBAR
// ============================================================
function initSidebar() {
    const toggle = document.querySelector('.header-hamburger');
    const sidebar = document.querySelector('.app-sidebar');

    if (!toggle || !sidebar) return;

    toggle.addEventListener('click', e => {
        e.preventDefault();
        toggleSidebar();
    });

    // Mobile overlay
    const overlay = document.querySelector('.sidebar-overlay');
    if (overlay) {
        overlay.addEventListener('click', () => toggleSidebar());
    }
}

function toggleSidebar() {
    const body = document.body;
    const sidebar = document.querySelector('.app-sidebar');
    const hamburger = document.querySelector('.header-hamburger');

    if (window.innerWidth <= 1199) {
        body.classList.toggle('sidebar-mobile-open');
        const isOpen = body.classList.contains('sidebar-mobile-open');
        if (sidebar) sidebar.setAttribute('aria-hidden', (!isOpen).toString());
        if (hamburger) hamburger.setAttribute('aria-expanded', isOpen.toString());
        // Focus management
        if (isOpen && sidebar) {
            sidebar.querySelector('.nav-link')?.focus();
        }
    } else {
        body.classList.toggle('sidebar-collapsed');
        const isOpen = !body.classList.contains('sidebar-collapsed');
        if (sidebar) sidebar.setAttribute('aria-hidden', (!isOpen).toString());
        if (hamburger) hamburger.setAttribute('aria-expanded', isOpen.toString());
    }
}

// ============================================================
// ACTIVE NAV — Expand submenu when child is active
// The 'active' class is managed server-side by ActiveRouteTagHelper.
// This function only ensures parent sub-menus are expanded.
// ============================================================
function initActiveNav() {
    // Expand parent collapse if child has active class (set by TagHelper)
    document.querySelectorAll('.nav-sub .nav-link.active').forEach(active => {
        const collapse = active.closest('.collapse');
        if (collapse) {
            const trigger = document.querySelector(`[data-bs-target="#${collapse.id}"]`) ||
                            document.querySelector(`[href="#${collapse.id}"]`);
            if (trigger) trigger.setAttribute('aria-expanded', 'true');
            collapse.classList.add('show');
        }
    });
}

// ============================================================
// NOTIFICATION POLLING
// ============================================================
function initNotificationPolling() {
    const badge = document.getElementById('notificationBadge');
    if (!badge) return;

    async function poll() {
        try {
            const res = await fetch('/ClinicAdmin/Notifications/Index', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const html = await res.text();
            const doc = new DOMParser().parseFromString(html, 'text/html');
            const count = doc.querySelector('[data-unread-count]');
            if (count) {
                const n = count.getAttribute('data-unread-count');
                badge.textContent = n;
                badge.style.display = n !== '0' ? 'flex' : 'none';
            }
        } catch (err) {
            console.debug('Notification poll failed');
        }
    }

    setInterval(poll, 60000);
}

// ============================================================
// PASSWORD TOGGLE
// ============================================================
function initPasswordToggle() {
    document.querySelectorAll('.toggle-password').forEach(btn => {
        btn.addEventListener('click', () => {
            const input = btn.closest('.input-group').querySelector('.form-control');
            if (!input) return;
            const isPassword = input.getAttribute('type') === 'password';
            input.setAttribute('type', isPassword ? 'text' : 'password');
            btn.querySelector('i').classList.toggle('fa-eye');
            btn.querySelector('i').classList.toggle('fa-eye-slash');
        });
    });
}

// ============================================================
// TABLE SEARCH
// ============================================================
function initTableSearch() {
    document.querySelectorAll('[data-table-search]').forEach(input => {
        const tableId = input.getAttribute('data-table-search');
        const table = document.getElementById(tableId);
        if (!table) return;

        input.addEventListener('keyup', () => {
            const filter = input.value.toUpperCase();
            const rows = table.querySelectorAll('tbody tr');

            rows.forEach(row => {
                const text = Array.from(row.querySelectorAll('td'))
                    .map(cell => cell.textContent.toUpperCase())
                    .join(' ');
                row.style.display = text.includes(filter) ? '' : 'none';
            });
        });
    });
}

// ============================================================
// LOADING BUTTONS
// ============================================================
function initLoadingButtons() {
    document.querySelectorAll('[data-loading]').forEach(btn => {
        const form = btn.closest('form');
        if (!form) return;

        form.addEventListener('submit', () => {
            btn.classList.add('loading');
            btn.disabled = true;
        });
    });
}

// ============================================================
// LANGUAGE TOGGLE
// ============================================================
function initLanguageToggle() {
    const buttons = document.querySelectorAll('.header-lang-btn');
    if (!buttons.length) return;

    buttons.forEach(btn => {
        btn.addEventListener('click', () => {
            const lang = btn.getAttribute('data-lang');
            if (lang) setLanguage(lang);
        });
    });

    // Sync
    const saved = localStorage.getItem('lang');
    const server = document.documentElement.lang || 'en';
    if (saved && saved !== server) setLanguage(saved);
}

function setLanguage(lang) {
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    localStorage.setItem('lang', lang);
    document.cookie = 'lang=' + lang + '; path=/; max-age=31536000';
    location.reload();
}

// ============================================================
// ALERT
// ============================================================
function showAlert(message, type = 'info') {
    const icons = { success: 'check-circle', danger: 'exclamation-circle', warning: 'exclamation-triangle' };
    const div = document.createElement('div');
    div.className = `alert alert-${type} alert-dismissible fade show`;
    div.setAttribute('role', 'alert');
    div.innerHTML = `
        <i class="fas fa-${icons[type] || 'info-circle'}"></i>
        <div class="alert-content">${message}</div>
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    const container = document.querySelector('.content-area');
    if (container) {
        container.insertBefore(div, container.firstChild);
    }

    setTimeout(() => { if (div.parentNode) div.remove(); }, 5000);
}

// ============================================================
// API HELPER
// ============================================================
async function apiCall(url, method = 'GET', data = null) {
    const options = {
        method,
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        }
    };

    if (data && (method === 'POST' || method === 'PUT')) {
        options.body = JSON.stringify(data);
    }

    try {
        const res = await fetch(url, options);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return await res.json();
    } catch (err) {
        console.error('API Error:', err);
        showAlert('An error occurred. Please try again.', 'danger');
        throw err;
    }
}

// ============================================================
// FORMATTING HELPERS
// ============================================================
function formatDate(date, format = 'dd/mm/yyyy') {
    const d = new Date(date);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    if (format === 'dd/mm/yyyy') return `${day}/${month}/${year}`;
    if (format === 'yyyy-mm-dd') return `${year}-${month}-${day}`;
    return d.toLocaleDateString();
}

function formatCurrency(amount, currency = 'SAR') {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(amount);
}

function formatPercentage(value, decimals = 0) {
    return Number(value).toFixed(decimals) + '%';
}

// ============================================================
// TABLE EXPORT / PRINT
// ============================================================
function exportTableToCSV(tableId, filename = 'export.csv') {
    const table = document.getElementById(tableId);
    if (!table) return;

    const rows = [];
    const headers = Array.from(table.querySelectorAll('thead th'))
        .map(th => `"${th.textContent.trim()}"`);
    rows.push(headers.join(','));

    table.querySelectorAll('tbody tr').forEach(row => {
        const cells = Array.from(row.querySelectorAll('td'))
            .map(td => `"${td.textContent.trim().replace(/"/g, '""')}"`);
        rows.push(cells.join(','));
    });

    const csv = rows.join('\n');
    const link = document.createElement('a');
    link.href = 'data:text/csv;charset=utf-8,' + encodeURIComponent(csv);
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
}

function printTable(tableId) {
    const table = document.getElementById(tableId);
    if (!table) return;
    const w = window.open('', '', 'width=900,height=600');
    w.document.write(`<html><head><title>Print</title>
        <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">
        <style>body{padding:20px;font-family:system-ui,sans-serif}</style>
        </head><body>${table.outerHTML}</body></html>`);
    w.document.close();
    w.print();
}

// ============================================================
// LOADING SPINNER
// ============================================================
function showLoading(element) {
    const el = typeof element === 'string' ? document.querySelector(element) : element;
    if (!el) return;
    el.innerHTML = '<span class="spinner spinner-sm" role="status"></span> Loading...';
    el.disabled = true;
}

function hideLoading(element, text = 'Submit') {
    const el = typeof element === 'string' ? document.querySelector(element) : element;
    if (!el) return;
    el.innerHTML = text;
    el.disabled = false;
}

// ============================================================
// DEVELOPER TOAST
// ============================================================
document.addEventListener('click', e => {
    const trigger = e.target.closest('[data-dev-card]');
    if (!trigger) return;
    e.preventDefault();

    const toast = document.getElementById('developerToast');
    if (!toast) return;

    toast.classList.remove('active');
    void toast.offsetWidth;
    toast.classList.add('active');

    setTimeout(() => toast.classList.remove('active'), 2800);
});

document.addEventListener('keydown', e => {
    if (e.key === 'Escape') {
        const toast = document.getElementById('developerToast');
        if (toast) toast.classList.remove('active');

        // Close mobile sidebar on Escape
        if (document.body.classList.contains('sidebar-mobile-open')) {
            toggleSidebar();
        }
    }
});

// ============================================================
// SIDEBAR KEYBOARD SUPPORT
// ============================================================
function initSidebarKeyboard() {
    const sidebar = document.querySelector('.app-sidebar');
    if (!sidebar) return;

    sidebar.addEventListener('keydown', e => {
        // Trap focus within sidebar when open on mobile
        if (e.key === 'Escape' && document.body.classList.contains('sidebar-mobile-open')) {
            toggleSidebar();
            const hamburger = document.querySelector('.header-hamburger');
            if (hamburger) hamburger.focus();
        }
    });

    // Sync aria-expanded on hamburger button
    const hamburger = document.querySelector('.header-hamburger');
    if (hamburger) {
        const observer = new MutationObserver(() => {
            const isOpen = document.body.classList.contains('sidebar-mobile-open') ||
                           !document.body.classList.contains('sidebar-collapsed');
            hamburger.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });
        observer.observe(document.body, { attributes: true, attributeFilter: ['class'] });
        // Initial sync
        observer.takeRecords();
    }
}

// ============================================================
// PASSWORD STRENGTH INDICATOR
// ============================================================
function initPasswordStrength() {
    const inputs = document.querySelectorAll('[data-password-strength]');
    if (!inputs.length) return;

    inputs.forEach(input => {
        const container = input.closest('.form-group').querySelector('.password-strength');
        if (!container) return;
        const bar = container.querySelector('.password-strength-bar');
        const text = container.querySelector('.password-strength-text');
        if (!bar) return;

        function evaluateStrength() {
            const val = input.value;
            let score = 0;

            if (val.length >= 8) score++;
            if (val.length >= 12) score++;
            if (/[A-Z]/.test(val) && /[a-z]/.test(val)) score++;
            if (/\d/.test(val) && /[^A-Za-z0-9]/.test(val)) score++;

            const widths = ['0%', '25%', '50%', '75%', '100%'];
            const colors = ['', 'var(--danger)', 'var(--warning)', 'var(--info)', 'var(--success)'];
            const labels = ['', 'Weak', 'Fair', 'Good', 'Strong'];
            const l10n = {
                'Weak': 'Weak',
                'Fair': 'Fair',
                'Good': 'Good',
                'Strong': 'Strong'
            };

            bar.style.setProperty('--strength-width', widths[score]);
            bar.style.setProperty('--strength-color', colors[score]);

            if (text) {
                text.textContent = score > 0 ? (l10n[labels[score]] || labels[score]) : '';
            }
            bar.setAttribute('aria-valuenow', score);
        }

        input.addEventListener('input', evaluateStrength);
        input.addEventListener('blur', evaluateStrength);
        evaluateStrength();
    });
}
