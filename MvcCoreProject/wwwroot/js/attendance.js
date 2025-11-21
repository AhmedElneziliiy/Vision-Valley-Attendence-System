// Attendance JavaScript Functions

// Format time in HH:mm format
function formatTime(date) {
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${hours}:${minutes}`;
}

// Format duration from minutes to "Xh Ym" format
function formatDuration(minutes) {
    if (minutes <= 0) return '0h 0m';

    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return `${hours}h ${mins}m`;
}

// Calculate duration between two time strings (HH:mm format)
function calculateDuration(startTime, endTime) {
    if (!startTime || !endTime) return 0;

    const [startHour, startMin] = startTime.split(':').map(Number);
    const [endHour, endMin] = endTime.split(':').map(Number);

    const startMinutes = startHour * 60 + startMin;
    const endMinutes = endHour * 60 + endMin;

    return endMinutes - startMinutes;
}

// Get badge class based on status
function getStatusBadgeClass(status) {
    switch (status) {
        case 'Present':
            return 'bg-success';
        case 'Absent':
            return 'bg-danger';
        case 'Checked In':
            return 'bg-warning';
        case 'Late':
            return 'bg-warning';
        case 'Early Leave':
            return 'bg-info';
        default:
            return 'bg-secondary';
    }
}

// Filter table rows based on search term
function filterTable(tableId, searchInputId) {
    const searchInput = document.getElementById(searchInputId);
    const table = document.getElementById(tableId);

    if (!searchInput || !table) return;

    searchInput.addEventListener('input', function (e) {
        const searchTerm = e.target.value.toLowerCase();
        const rows = table.querySelectorAll('tbody tr');

        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            row.style.display = text.includes(searchTerm) ? '' : 'none';
        });
    });
}

// Export table to CSV
function exportTableToCSV(tableId, filename) {
    const table = document.getElementById(tableId);
    if (!table) {
        console.error('Table not found');
        return;
    }

    const rows = Array.from(table.querySelectorAll('tr'));
    const csv = rows.map(row => {
        const cells = Array.from(row.querySelectorAll('th, td'));
        return cells.map(cell => {
            // Get text content and clean it
            const text = cell.textContent.trim().replace(/"/g, '""');
            return `"${text}"`;
        }).join(',');
    }).join('\n');

    // Create blob and download
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Show loading spinner
function showLoading(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = `
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `;
    }
}

// Show error message
function showError(elementId, message) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = `
            <div class="alert alert-danger" role="alert">
                <i class="bi bi-exclamation-triangle-fill"></i>
                ${message}
            </div>
        `;
    }
}

// Show success message
function showSuccess(elementId, message) {
    const element = document.getElementById(elementId);
    if (element) {
        element.innerHTML = `
            <div class="alert alert-success" role="alert">
                <i class="bi bi-check-circle-fill"></i>
                ${message}
            </div>
        `;
    }
}

// Validate date range
function validateDateRange(startDateId, endDateId) {
    const startDate = document.getElementById(startDateId);
    const endDate = document.getElementById(endDateId);

    if (!startDate || !endDate) return true;

    const start = new Date(startDate.value);
    const end = new Date(endDate.value);

    if (start > end) {
        alert('Start date cannot be after end date');
        return false;
    }

    return true;
}

// Format date to YYYY-MM-DD
function formatDate(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

// Get date range for common periods
function getDateRange(period) {
    const today = new Date();
    const startDate = new Date();
    const endDate = new Date();

    switch (period) {
        case 'today':
            startDate.setDate(today.getDate());
            break;
        case 'yesterday':
            startDate.setDate(today.getDate() - 1);
            endDate.setDate(today.getDate() - 1);
            break;
        case 'week':
            startDate.setDate(today.getDate() - today.getDay());
            break;
        case 'lastWeek':
            startDate.setDate(today.getDate() - today.getDay() - 7);
            endDate.setDate(today.getDate() - today.getDay() - 1);
            break;
        case 'month':
            startDate.setDate(1);
            break;
        case 'lastMonth':
            startDate.setMonth(today.getMonth() - 1, 1);
            endDate.setMonth(today.getMonth(), 0);
            break;
        case '7days':
            startDate.setDate(today.getDate() - 7);
            break;
        case '30days':
            startDate.setDate(today.getDate() - 30);
            break;
        case 'quarter':
            const quarter = Math.floor(today.getMonth() / 3);
            startDate.setMonth(quarter * 3, 1);
            break;
        case 'year':
            startDate.setMonth(0, 1);
            break;
        default:
            break;
    }

    return {
        start: formatDate(startDate),
        end: formatDate(endDate)
    };
}

// Set date range inputs
function setDateRangeInputs(startInputId, endInputId, period) {
    const dateRange = getDateRange(period);
    const startInput = document.getElementById(startInputId);
    const endInput = document.getElementById(endInputId);

    if (startInput) startInput.value = dateRange.start;
    if (endInput) endInput.value = dateRange.end;
}

// Update clock display
function updateClock(clockElementId) {
    const clockElement = document.getElementById(clockElementId);
    if (!clockElement) return;

    function update() {
        const now = new Date();
        const hours = String(now.getHours()).padStart(2, '0');
        const minutes = String(now.getMinutes()).padStart(2, '0');
        const seconds = String(now.getSeconds()).padStart(2, '0');
        clockElement.textContent = `${hours}:${minutes}:${seconds}`;
    }

    update();
    setInterval(update, 1000);
}

// Confirm action
function confirmAction(message, callback) {
    if (confirm(message)) {
        if (typeof callback === 'function') {
            callback();
        }
        return true;
    }
    return false;
}

// Parse time string to minutes
function parseTimeToMinutes(timeString) {
    if (!timeString) return 0;

    const [hours, minutes] = timeString.split(':').map(Number);
    return hours * 60 + minutes;
}

// Calculate working hours based on records
function calculateWorkingHours(records) {
    if (!records || records.length === 0) return 0;

    let totalMinutes = 0;
    let lastCheckIn = null;

    records.forEach(record => {
        if (record.IsCheckIn) {
            lastCheckIn = record.Time;
        } else if (lastCheckIn) {
            const checkInMinutes = parseTimeToMinutes(lastCheckIn);
            const checkOutMinutes = parseTimeToMinutes(record.Time);
            totalMinutes += checkOutMinutes - checkInMinutes;
            lastCheckIn = null;
        }
    });

    return totalMinutes;
}

// Get attendance status color
function getAttendanceStatusColor(status) {
    switch (status) {
        case 'Present':
            return '#198754'; // Success green
        case 'Absent':
            return '#dc3545'; // Danger red
        case 'Checked In':
            return '#ffc107'; // Warning yellow
        case 'Late':
            return '#fd7e14'; // Orange
        case 'Early Leave':
            return '#0dcaf0'; // Info cyan
        default:
            return '#6c757d'; // Secondary gray
    }
}

// Initialize tooltips (Bootstrap 5)
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        initializeTooltips();
    }

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});

// Export functions for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        formatTime,
        formatDuration,
        calculateDuration,
        getStatusBadgeClass,
        filterTable,
        exportTableToCSV,
        showLoading,
        showError,
        showSuccess,
        validateDateRange,
        formatDate,
        getDateRange,
        setDateRangeInputs,
        updateClock,
        confirmAction,
        parseTimeToMinutes,
        calculateWorkingHours,
        getAttendanceStatusColor,
        initializeTooltips
    };
}
