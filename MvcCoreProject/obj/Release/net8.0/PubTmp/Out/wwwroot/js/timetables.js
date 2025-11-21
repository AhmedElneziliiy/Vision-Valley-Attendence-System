// Timetables Management JavaScript

// Filter timetables
function filterTimetables() {
    const statusFilter = document.getElementById('statusFilter')?.value.toLowerCase();
    const branchFilter = document.getElementById('branchFilter')?.value;
    const userCountFilter = document.getElementById('userCountFilter')?.value;
    const searchText = document.getElementById('searchTimetable')?.value.toLowerCase() || '';

    const table = document.getElementById('timetablesTable');
    if (!table) return;

    const rows = table.querySelectorAll('tbody tr');
    let visibleCount = 0;

    rows.forEach(row => {
        let show = true;

        // Status filter
        if (statusFilter) {
            const rowStatus = row.getAttribute('data-status');
            if (rowStatus !== statusFilter) {
                show = false;
            }
        }

        // Branch filter
        if (branchFilter && show) {
            const rowBranchId = row.getAttribute('data-branch-id');
            if (rowBranchId !== branchFilter) {
                show = false;
            }
        }

        // User count filter
        if (userCountFilter && show) {
            const userCount = parseInt(row.getAttribute('data-user-count'));

            if (userCountFilter === '0' && userCount !== 0) {
                show = false;
            } else if (userCountFilter === '1-10' && (userCount < 1 || userCount > 10)) {
                show = false;
            } else if (userCountFilter === '11-50' && (userCount < 11 || userCount > 50)) {
                show = false;
            } else if (userCountFilter === '51+' && userCount < 51) {
                show = false;
            }
        }

        // Search filter
        if (searchText && show) {
            const timetableName = row.getAttribute('data-timetable-name') || '';
            if (!timetableName.includes(searchText)) {
                show = false;
            }
        }

        row.style.display = show ? '' : 'none';
        if (show) visibleCount++;
    });

    // Show message if no results
    updateNoResultsMessage(visibleCount);
}

// Update no results message
function updateNoResultsMessage(visibleCount) {
    const table = document.getElementById('timetablesTable');
    if (!table) return;

    let noResultsRow = table.querySelector('.no-results-row');

    if (visibleCount === 0) {
        if (!noResultsRow) {
            const tbody = table.querySelector('tbody');
            const colCount = table.querySelectorAll('thead th').length;

            noResultsRow = document.createElement('tr');
            noResultsRow.className = 'no-results-row';
            noResultsRow.innerHTML = `
                <td colspan="${colCount}" class="text-center py-5">
                    <i class="bi bi-search" style="font-size: 3rem; color: #6c757d;"></i>
                    <p class="mt-2 mb-0 text-muted">No timetables match your filters</p>
                </td>
            `;
            tbody.appendChild(noResultsRow);
        }
        noResultsRow.style.display = '';
    } else if (noResultsRow) {
        noResultsRow.style.display = 'none';
    }
}

// View timetable details
function viewTimetable(timetableId) {
    window.location.href = `/Timetables/Details/${timetableId}`;
}

// Edit timetable
function editTimetable(timetableId) {
    window.location.href = `/Timetables/Edit/${timetableId}`;
}

// Confirm delete timetable
function confirmDelete(timetableId, timetableName) {
    if (confirm(`Are you sure you want to delete timetable "${timetableName}"?\n\nThis will deactivate the timetable. Users assigned to this timetable will not be affected.`)) {
        deleteTimetable(timetableId);
    }
}

// Delete timetable
async function deleteTimetable(timetableId) {
    try {
        // Get the anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const formData = new FormData();
        formData.append('id', timetableId);
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        const response = await fetch(`/Timetables/Delete/${timetableId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        });

        if (response.ok) {
            showToast('Timetable deactivated successfully', 'success');
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast('Failed to deactivate timetable', 'error');
        }
    } catch (error) {
        console.error('Error deactivating timetable:', error);
        showToast('An error occurred while deactivating timetable', 'error');
    }
}

// Toast notification
function showToast(message, type = 'info') {
    // Remove existing toasts
    const existingToast = document.querySelector('.toast-notification');
    if (existingToast) {
        existingToast.remove();
    }

    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;

    const iconMap = {
        success: 'check-circle-fill',
        error: 'exclamation-circle-fill',
        info: 'info-circle-fill',
        warning: 'exclamation-triangle-fill'
    };

    toast.innerHTML = `
        <div class="toast-content">
            <i class="bi bi-${iconMap[type] || 'info-circle-fill'}"></i>
            <span>${message}</span>
        </div>
    `;

    document.body.appendChild(toast);
    setTimeout(() => toast.classList.add('show'), 100);
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Form validation enhancement
document.addEventListener('DOMContentLoaded', function () {
    const createForm = document.getElementById('createTimetableForm');
    const editForm = document.getElementById('editTimetableForm');

    if (createForm) {
        createForm.addEventListener('submit', function (e) {
            const submitBtn = createForm.querySelector('.btn-submit');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="bi bi-hourglass-split"></i><span>Creating...</span>';
            }
        });
    }

    if (editForm) {
        editForm.addEventListener('submit', function (e) {
            const submitBtn = editForm.querySelector('.btn-submit');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="bi bi-hourglass-split"></i><span>Updating...</span>';
            }
        });
    }

    // Initialize filters on Index page
    const timetablesTable = document.getElementById('timetablesTable');
    if (timetablesTable) {
        const statusFilter = document.getElementById('statusFilter');
        const branchFilter = document.getElementById('branchFilter');
        const userCountFilter = document.getElementById('userCountFilter');
        const searchTimetable = document.getElementById('searchTimetable');

        if (statusFilter) statusFilter.addEventListener('change', filterTimetables);
        if (branchFilter) branchFilter.addEventListener('change', filterTimetables);
        if (userCountFilter) userCountFilter.addEventListener('change', filterTimetables);
        if (searchTimetable) searchTimetable.addEventListener('input', filterTimetables);
    }

    // Auto-dismiss alerts
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});
