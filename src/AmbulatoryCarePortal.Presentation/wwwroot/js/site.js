// CBAHI Portal - Enterprise JavaScript
// Healthcare SaaS Platform | 2026

document.addEventListener('DOMContentLoaded', function () {
    initializeTooltips();
    initializeConfirmation();
    initializeSidebar();
    initializeActiveNav();
    initializeNotificationPolling();
    initializePasswordToggle();
    initializeTableSearch();
    initializeLoadingButtons();
    initializeLanguageToggle();
});

// ============================================================
// TOOLTIPS
// ============================================================
function initializeTooltips() {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        new bootstrap.Tooltip(el);
    });
}

// ============================================================
// CONFIRMATION DIALOGS
// ============================================================
function initializeConfirmation() {
    document.addEventListener('click', function (event) {
        var target = event.target.closest('[data-confirm]');
        if (!target) return;
        event.preventDefault();
        var message = target.getAttribute('data-confirm');
        if (confirm(message)) {
            if (target.tagName === 'A') {
                window.location.href = target.href;
            } else if (target.tagName === 'BUTTON' || target.closest('form')) {
                var form = target.closest('form');
                if (form) form.submit();
            }
        }
    });
}

// ============================================================
// SIDEBAR
// ============================================================
function initializeSidebar() {
    var toggleBtn = document.querySelector('.header-hamburger');
    var sidebar = document.querySelector('.main-sidebar');

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function (e) {
            e.preventDefault();
            toggleSidebar();
        });
    }

    // Mobile overlay
    if (sidebar && window.innerWidth <= 991) {
        var overlay = document.createElement('div');
        overlay.className = 'sidebar-overlay';
        document.body.appendChild(overlay);

        overlay.addEventListener('click', function () {
            toggleSidebar();
        });

        sidebar.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    }
}

function toggleSidebar() {
    var body = document.body;
    if (window.innerWidth <= 991) {
        body.classList.toggle('sidebar-open');
    } else {
        body.classList.toggle('sidebar-collapse');
    }
}

// ============================================================
// ACTIVE NAV LINK
// ============================================================
function initializeActiveNav() {
    var currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll('.nav-sidebar .nav-link').forEach(function (link) {
        var href = link.getAttribute('href');
        if (href && currentPath.indexOf(href.toLowerCase()) === 0) {
            link.classList.add('active');
        }
    });
}

// ============================================================
// NOTIFICATION POLLING
// ============================================================
function initializeNotificationPolling() {
    var badge = document.getElementById('notificationBadge');
    if (!badge) return;

    function pollNotifications() {
        fetch('/ClinicAdmin/Notifications/Index', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (r) { return r.text(); })
            .then(function (html) {
                var parser = new DOMParser();
                var doc = parser.parseFromString(html, 'text/html');
                var count = doc.querySelector('[data-unread-count]');
                if (count) {
                    badge.textContent = count.getAttribute('data-unread-count');
                    if (count.getAttribute('data-unread-count') !== '0') {
                        badge.style.display = 'flex';
                    } else {
                        badge.style.display = 'none';
                    }
                }
            })
            .catch(function () { });
    }

    setInterval(pollNotifications, 60000);
}

// ============================================================
// PASSWORD TOGGLE (Login/Reset)
// ============================================================
function initializePasswordToggle() {
    document.querySelectorAll('.toggle-password').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var input = this.closest('.input-group').querySelector('.form-control');
            if (!input) return;
            var type = input.getAttribute('type') === 'password' ? 'text' : 'password';
            input.setAttribute('type', type);
            this.querySelector('i').classList.toggle('fa-eye');
            this.querySelector('i').classList.toggle('fa-eye-slash');
        });
    });
}

// ============================================================
// TABLE SEARCH
// ============================================================
function initializeTableSearch() {
    document.querySelectorAll('[data-table-search]').forEach(function (input) {
        var tableId = input.getAttribute('data-table-search');
        var table = document.getElementById(tableId);
        if (!table) return;

        input.addEventListener('keyup', function () {
            var filter = this.value.toUpperCase();
            var rows = table.getElementsByTagName('tr');

            for (var i = 1; i < rows.length; i++) {
                var cells = rows[i].getElementsByTagName('td');
                var found = false;
                for (var j = 0; j < cells.length; j++) {
                    if (cells[j] && cells[j].textContent.toUpperCase().indexOf(filter) > -1) {
                        found = true;
                        break;
                    }
                }
                rows[i].style.display = found ? '' : 'none';
            }
        });
    });
}

// ============================================================
// LOADING BUTTONS
// ============================================================
function initializeLoadingButtons() {
    document.querySelectorAll('[data-loading]').forEach(function (btn) {
        var form = btn.closest('form');
        if (form) {
            form.addEventListener('submit', function () {
                btn.classList.add('loading');
                btn.disabled = true;
            });

            // Re-enable button if jQuery Validate blocks submission
            if (typeof $ !== 'undefined') {
                $(form).on('invalid-form.validate', function () {
                    btn.classList.remove('loading');
                    btn.disabled = false;
                });
            }
        }
    });
}

// ============================================================
// LANGUAGE TOGGLE (Segmented Control)
// ============================================================
function initializeLanguageToggle() {
    var langOptions = document.querySelectorAll('.header-lang-option');
    if (!langOptions.length) return;

    langOptions.forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            var lang = this.getAttribute('data-lang');
            if (!lang) return;
            setLanguage(lang);
        });
    });

    // Sync client state with server-rendered language
    var savedLang = localStorage.getItem('lang') || 'en';
    var serverLang = document.documentElement.lang || 'en';
    if (savedLang !== serverLang) {
        setLanguage(savedLang);
    }

    // Legacy single-toggle fallback
    var oldToggle = document.getElementById('langToggle');
    if (oldToggle) {
        oldToggle.style.display = 'none';
    }
}

function setLanguage(lang) {
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    localStorage.setItem('lang', lang);
    document.cookie = 'lang=' + lang + '; path=/; max-age=31536000';

    // Reload so server renders translations in the new language
    location.reload();
}

// ============================================================
// SHOW ALERT
// ============================================================
function showAlert(message, type) {
    type = type || 'info';
    var alertDiv = document.createElement('div');
    alertDiv.className = 'alert alert-' + type + ' alert-dismissible fade show';
    alertDiv.setAttribute('role', 'alert');
    alertDiv.style.animation = 'fadeIn 0.3s ease-out';
    alertDiv.innerHTML =
        '<i class="fas fa-' + (type === 'success' ? 'check-circle' : type === 'danger' ? 'exclamation-circle' : type === 'warning' ? 'exclamation-triangle' : 'info-circle') + '"></i>' +
        '<div class="alert-content">' + message + '</div>' +
        '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>';

    var container = document.querySelector('.content > .container-fluid');
    if (container) {
        container.insertBefore(alertDiv, container.firstChild);
    }

    setTimeout(function () {
        if (alertDiv.parentNode) alertDiv.remove();
    }, 5000);
}

// ============================================================
// API CALL HELPER
// ============================================================
async function apiCall(url, method, data) {
    method = method || 'GET';
    var options = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        }
    };

    if (data && (method === 'POST' || method === 'PUT')) {
        options.body = JSON.stringify(data);
    }

    try {
        var response = await fetch(url, options);
        if (!response.ok) {
            throw new Error('HTTP error! status: ' + response.status);
        }
        return await response.json();
    } catch (error) {
        console.error('API Call Error:', error);
        showAlert('An error occurred. Please try again.', 'danger');
        throw error;
    }
}

// ============================================================
// DATE FORMATTING
// ============================================================
function formatDate(date, format) {
    format = format || 'dd/mm/yyyy';
    var d = new Date(date);
    var day = String(d.getDate()).padStart(2, '0');
    var month = String(d.getMonth() + 1).padStart(2, '0');
    var year = d.getFullYear();

    if (format === 'dd/mm/yyyy') return day + '/' + month + '/' + year;
    if (format === 'yyyy-mm-dd') return year + '-' + month + '-' + day;
    return d.toLocaleDateString();
}

// ============================================================
// CURRENCY FORMATTING
// ============================================================
function formatCurrency(amount, currency) {
    currency = currency || 'SAR';
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency
    }).format(amount);
}

// ============================================================
// PERCENTAGE FORMATTING
// ============================================================
function formatPercentage(value, decimals) {
    decimals = decimals || 0;
    return Number(value).toFixed(decimals) + '%';
}

// ============================================================
// EXPORT TABLE TO CSV
// ============================================================
function exportTableToCSV(tableId, filename) {
    filename = filename || 'export.csv';
    var table = document.getElementById(tableId);
    if (!table) return;

    var csv = [];
    var headers = [];
    table.querySelectorAll('thead th').forEach(function (th) {
        headers.push('"' + th.textContent.trim() + '"');
    });
    csv.push(headers.join(','));

    table.querySelectorAll('tbody tr').forEach(function (row) {
        var cells = [];
        row.querySelectorAll('td').forEach(function (td) {
            cells.push('"' + td.textContent.trim().replace(/"/g, '""') + '"');
        });
        csv.push(cells.join(','));
    });

    var csvContent = csv.join('\n');
    var link = document.createElement('a');
    link.href = 'data:text/csv;charset=utf-8,' + encodeURIComponent(csvContent);
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// ============================================================
// PRINT TABLE
// ============================================================
function printTable(tableId) {
    var table = document.getElementById(tableId);
    if (!table) return;

    var printWindow = window.open('', '', 'width=900,height=600');
    printWindow.document.write('<html><head><title>Print</title>');
    printWindow.document.write('<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">');
    printWindow.document.write('<style>body{padding:20px;font-family:Segoe UI,sans-serif}</style>');
    printWindow.document.write('</head><body>');
    printWindow.document.write(table.outerHTML);
    printWindow.document.write('</body></html>');
    printWindow.document.close();
    printWindow.print();
}

// ============================================================
// SHOW/HIDE LOADING SPINNER
// ============================================================
function showLoading(element) {
    if (typeof element === 'string') {
        element = document.querySelector(element);
    }
    if (element) {
        element.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...';
        element.disabled = true;
    }
}

function hideLoading(element, text) {
    text = text || 'Submit';
    if (typeof element === 'string') {
        element = document.querySelector(element);
    }
    if (element) {
        element.innerHTML = text;
        element.disabled = false;
    }
}

// ============================================================
// DEVELOPER TOAST (auto-dismiss)
// ============================================================
var developerToastTimer = null;

document.addEventListener('click', function (e) {
    var trigger = e.target.closest('[data-dev-card]');
    if (!trigger) return;
    e.preventDefault();

    var toast = document.getElementById('developerToast');
    if (!toast) return;

    if (developerToastTimer) {
        clearTimeout(developerToastTimer);
        developerToastTimer = null;
    }

    toast.classList.remove('active');
    void toast.offsetWidth;
    toast.classList.add('active');

    developerToastTimer = setTimeout(function () {
        toast.classList.remove('active');
        developerToastTimer = null;
    }, 2800);
});

document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        var toast = document.getElementById('developerToast');
        if (toast) {
            toast.classList.remove('active');
            if (developerToastTimer) {
                clearTimeout(developerToastTimer);
                developerToastTimer = null;
            }
        }
    }
});
