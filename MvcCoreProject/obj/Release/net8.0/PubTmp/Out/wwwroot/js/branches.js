// Branches Management JavaScript

// Filter branches
function filterBranches() {
    const statusFilter = document.getElementById('statusFilter')?.value.toLowerCase();
    const typeFilter = document.getElementById('typeFilter')?.value.toLowerCase();
    const userCountFilter = document.getElementById('userCountFilter')?.value;
    const searchText = document.getElementById('searchBranch')?.value.toLowerCase() || '';

    const table = document.getElementById('branchesTable');
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

        // Type filter
        if (typeFilter && show) {
            const rowType = row.getAttribute('data-type');
            if (rowType !== typeFilter) {
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
            } else if (userCountFilter === '51-100' && (userCount < 51 || userCount > 100)) {
                show = false;
            } else if (userCountFilter === '100+' && userCount < 100) {
                show = false;
            }
        }

        // Search filter
        if (searchText && show) {
            const branchName = row.getAttribute('data-branch-name') || '';
            if (!branchName.includes(searchText)) {
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
    const table = document.getElementById('branchesTable');
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
                    <p class="mt-2 mb-0 text-muted">No branches match your filters</p>
                </td>
            `;
            tbody.appendChild(noResultsRow);
        }
        noResultsRow.style.display = '';
    } else if (noResultsRow) {
        noResultsRow.style.display = 'none';
    }
}

// View branch details
function viewBranch(branchId) {
    window.location.href = `/Branches/Details/${branchId}`;
}

// Edit branch
function editBranch(branchId) {
    window.location.href = `/Branches/Edit/${branchId}`;
}

// Confirm delete branch
function confirmDelete(branchId, branchName) {
    if (confirm(`Are you sure you want to delete branch "${branchName}"?\n\nThis will deactivate the branch. Users assigned to this branch will not be affected.`)) {
        deleteBranch(branchId);
    }
}

// Delete branch
async function deleteBranch(branchId) {
    try {
        // Get the anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const formData = new FormData();
        formData.append('id', branchId);
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        const response = await fetch(`/Branches/Delete/${branchId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        });

        if (response.ok) {
            showToast('Branch deactivated successfully', 'success');
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast('Failed to deactivate branch', 'error');
        }
    } catch (error) {
        console.error('Error deactivating branch:', error);
        showToast('An error occurred while deactivating branch', 'error');
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
    const createForm = document.getElementById('createBranchForm');
    const editForm = document.getElementById('editBranchForm');

    if (createForm) {
        createForm.addEventListener('submit', function (e) {
            const submitBtn = createForm.querySelector('.btn-submit');
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="bi bi-hourglass-split"></i><span>Creating...</span>';
        });
    }

    if (editForm) {
        editForm.addEventListener('submit', function (e) {
            const submitBtn = editForm.querySelector('.btn-submit');
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="bi bi-hourglass-split"></i><span>Updating...</span>';
        });
    }

    // Initialize filters on Index page
    const branchesTable = document.getElementById('branchesTable');
    if (branchesTable) {
        const statusFilter = document.getElementById('statusFilter');
        const typeFilter = document.getElementById('typeFilter');
        const userCountFilter = document.getElementById('userCountFilter');
        const searchBranch = document.getElementById('searchBranch');

        if (statusFilter) statusFilter.addEventListener('change', filterBranches);
        if (typeFilter) typeFilter.addEventListener('change', filterBranches);
        if (userCountFilter) userCountFilter.addEventListener('change', filterBranches);
        if (searchBranch) searchBranch.addEventListener('input', filterBranches);
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
