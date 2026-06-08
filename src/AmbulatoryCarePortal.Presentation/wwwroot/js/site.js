// CBAHI Portal - Common JavaScript Functions

document.addEventListener('DOMContentLoaded', function() {
    initializeTooltips();
    initializeConfirmation();
    initializeDataTables();
});

// Initialize Tooltips
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function(tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Initialize Confirmation Dialogs
function initializeConfirmation() {
    document.addEventListener('click', function(event) {
        if (event.target.matches('[data-confirm]')) {
            event.preventDefault();
            const message = event.target.getAttribute('data-confirm');
            
            if (confirm(message)) {
                if (event.target.tagName === 'A') {
                    window.location.href = event.target.href;
                } else if (event.target.tagName === 'BUTTON') {
                    event.target.closest('form').submit();
                }
            }
        }
    });
}

// Initialize Data Tables
function initializeDataTables() {
    const tables = document.querySelectorAll('[data-datatable]');
    tables.forEach(function(table) {
        // Custom data table initialization can be added here
    });
}

// Show Alert Message
function showAlert(message, type = 'info') {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show animated`;
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    const container = document.querySelector('.container-fluid');
    if (container) {
        container.insertBefore(alertDiv, container.firstChild);
    }
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        alertDiv.remove();
    }, 5000);
}

// API Call Helper
async function apiCall(url, method = 'GET', data = null) {
    const options = {
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
        const response = await fetch(url, options);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    } catch (error) {
        console.error('API Call Error:', error);
        showAlert('An error occurred. Please try again.', 'danger');
        throw error;
    }
}

// Format Date
function formatDate(date, format = 'dd/mm/yyyy') {
    const d = new Date(date);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    
    if (format === 'dd/mm/yyyy') return `${day}/${month}/${year}`;
    if (format === 'yyyy-mm-dd') return `${year}-${month}-${day}`;
    return d.toLocaleDateString();
}

// Format Currency
function formatCurrency(amount, currency = 'SAR') {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency
    }).format(amount);
}

// Format Percentage
function formatPercentage(value, decimals = 2) {
    return Number(value).toFixed(decimals) + '%';
}

// Update Compliance Score
async function updateComplianceScore() {
    try {
        const response = await apiCall('/ClinicAdmin/Dashboard/UpdateComplianceScore', 'POST');
        
        if (response.success) {
            showAlert('Compliance score updated successfully', 'success');
            return response.score;
        }
    } catch (error) {
        showAlert('Failed to update compliance score', 'danger');
    }
}

// Delete Record with Confirmation
async function deleteRecord(id, recordType = 'record') {
    if (confirm(`Are you sure you want to delete this ${recordType}?`)) {
        try {
            const response = await apiCall(`/api/${recordType}/${id}`, 'DELETE');
            
            if (response.success) {
                showAlert(`${recordType} deleted successfully`, 'success');
                setTimeout(() => window.location.reload(), 1500);
            }
        } catch (error) {
            showAlert(`Failed to delete ${recordType}`, 'danger');
        }
    }
}

// Search Table
function searchTable(inputId, tableId) {
    const input = document.getElementById(inputId);
    const table = document.getElementById(tableId);
    const rows = table.getElementsByTagName('tr');
    
    input.addEventListener('keyup', function() {
        const filter = input.value.toUpperCase();
        
        for (let i = 1; i < rows.length; i++) {
            const cells = rows[i].getElementsByTagName('td');
            let found = false;
            
            for (let j = 0; j < cells.length; j++) {
                if (cells[j].textContent.toUpperCase().indexOf(filter) > -1) {
                    found = true;
                    break;
                }
            }
            
            rows[i].style.display = found ? '' : 'none';
        }
    });
}

// Export Table to CSV
function exportTableToCSV(tableId, filename = 'export.csv') {
    const table = document.getElementById(tableId);
    let csv = [];
    
    // Get headers
    const headers = [];
    const headerCells = table.querySelectorAll('thead th');
    headerCells.forEach(th => headers.push(th.textContent.trim()));
    csv.push(headers.join(','));
    
    // Get rows
    const rows = table.querySelectorAll('tbody tr');
    rows.forEach(row => {
        const cells = [];
        const tds = row.querySelectorAll('td');
        tds.forEach(td => cells.push('"' + td.textContent.trim() + '"'));
        csv.push(cells.join(','));
    });
    
    // Download
    const csvContent = csv.join('\n');
    const link = document.createElement('a');
    link.href = 'data:text/csv;charset=utf-8,' + encodeURIComponent(csvContent);
    link.download = filename;
    link.click();
}

// Print Table
function printTable(tableId) {
    const table = document.getElementById(tableId);
    const printWindow = window.open('', '', 'width=900,height=600');
    
    printWindow.document.write('<html><head><title>Print</title>');
    printWindow.document.write('<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">');
    printWindow.document.write('</head><body>');
    printWindow.document.write(table.outerHTML);
    printWindow.document.write('</body></html>');
    
    printWindow.document.close();
    printWindow.print();
}

// Initialize Sidebar Toggle
document.addEventListener('DOMContentLoaded', function() {
    const sidebar = document.querySelector('.main-sidebar');
    const toggleBtn = document.querySelector('[data-widget="pushmenu"]');
    
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', function(e) {
            e.preventDefault();
            document.body.classList.toggle('sidebar-collapse');
        });
    }
});

// Add Active Class to Current Nav Link
document.addEventListener('DOMContentLoaded', function() {
    const currentUrl = window.location.pathname;
    const navLinks = document.querySelectorAll('.nav-link');
    
    navLinks.forEach(link => {
        if (link.href === window.location.href) {
            link.classList.add('active');
        }
    });
});

// Utility function to show loading spinner
function showLoading(element) {
    if (element instanceof string) {
        element = document.querySelector(element);
    }
    
    if (element) {
        element.innerHTML = '<div class="spinner-border spinner-border-sm" role="status"><span class="visually-hidden">Loading...</span></div> Loading...';
        element.disabled = true;
    }
}

// Utility function to hide loading spinner
function hideLoading(element, text = 'Submit') {
    if (element instanceof string) {
        element = document.querySelector(element);
    }
    
    if (element) {
        element.innerHTML = text;
        element.disabled = false;
    }
}
