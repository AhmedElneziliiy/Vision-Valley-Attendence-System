// Devices Management JavaScript

// Filter devices
function filterDevices() {
    const typeFilter = document.getElementById('typeFilter')?.value;
    const branchFilter = document.getElementById('branchFilter')?.value;
    const signInFilter = document.getElementById('signInFilter')?.value.toLowerCase();
    const searchText = document.getElementById('searchDevice')?.value.toLowerCase() || '';

    const table = document.getElementById('devicesTable');
    if (!table) return;

    const rows = table.querySelectorAll('tbody tr');
    let visibleCount = 0;

    rows.forEach(row => {
        let show = true;

        // Type filter
        if (typeFilter) {
            const rowType = row.getAttribute('data-device-type');
            if (rowType !== typeFilter) {
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

        // Sign in filter
        if (signInFilter && show) {
            const rowSignIn = row.getAttribute('data-sign-in');
            if (rowSignIn !== signInFilter) {
                show = false;
            }
        }

        // Search filter
        if (searchText && show) {
            const description = row.getAttribute('data-description') || '';
            if (!description.includes(searchText)) {
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
    const table = document.getElementById('devicesTable');
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
                    <p class="mt-2 mb-0 text-muted">No devices match your filters</p>
                </td>
            `;
            tbody.appendChild(noResultsRow);
        }
        noResultsRow.style.display = '';
    } else if (noResultsRow) {
        noResultsRow.style.display = 'none';
    }
}

// View device details
function viewDevice(deviceId) {
    window.location.href = `/Devices/Details/${deviceId}`;
}

// Edit device
function editDevice(deviceId) {
    window.location.href = `/Devices/Edit/${deviceId}`;
}

// Confirm delete device
function confirmDelete(deviceId) {
    if (confirm(`Are you sure you want to delete Device #${deviceId}?\n\nThis action cannot be undone.`)) {
        deleteDevice(deviceId);
    }
}

// Delete device
async function deleteDevice(deviceId) {
    try {
        // Get the anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const formData = new FormData();
        formData.append('id', deviceId);
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        const response = await fetch(`/Devices/Delete/${deviceId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        });

        if (response.ok) {
            showToast('Device deleted successfully', 'success');
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast('Failed to delete device', 'error');
        }
    } catch (error) {
        console.error('Error deleting device:', error);
        showToast('An error occurred while deleting device', 'error');
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
    const createForm = document.getElementById('createDeviceForm');
    const editForm = document.getElementById('editDeviceForm');

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
    const devicesTable = document.getElementById('devicesTable');
    if (devicesTable) {
        const typeFilter = document.getElementById('typeFilter');
        const branchFilter = document.getElementById('branchFilter');
        const signInFilter = document.getElementById('signInFilter');
        const searchDevice = document.getElementById('searchDevice');

        if (typeFilter) typeFilter.addEventListener('change', filterDevices);
        if (branchFilter) branchFilter.addEventListener('change', filterDevices);
        if (signInFilter) signInFilter.addEventListener('change', filterDevices);
        if (searchDevice) searchDevice.addEventListener('input', filterDevices);
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
