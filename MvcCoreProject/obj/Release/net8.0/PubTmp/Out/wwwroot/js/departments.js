// Departments Management JavaScript

// Populate branch filter dropdown
function populateBranchFilter() {
    const branchFilter = document.getElementById('branchFilter');
    if (!branchFilter) return;

    const table = document.getElementById('departmentsTable');
    if (!table) return;

    const branches = new Set();
    const rows = table.querySelectorAll('tbody tr');

    rows.forEach(row => {
        const branchName = row.getAttribute('data-branch');
        const branchId = row.getAttribute('data-branch-id');
        if (branchName && branchId) {
            branches.add(JSON.stringify({ id: branchId, name: branchName }));
        }
    });

    // Clear existing options except "All Branches"
    branchFilter.innerHTML = '<option value="">All Branches</option>';

    // Add unique branches
    Array.from(branches).forEach(branchJson => {
        const branch = JSON.parse(branchJson);
        const option = document.createElement('option');
        option.value = branch.id;
        option.textContent = branch.name;
        branchFilter.appendChild(option);
    });
}

// Filter departments
function filterDepartments() {
    const branchFilter = document.getElementById('branchFilter')?.value.toLowerCase();
    const statusFilter = document.getElementById('statusFilter')?.value.toLowerCase();
    const userCountFilter = document.getElementById('userCountFilter')?.value;
    const searchText = document.getElementById('searchDepartment')?.value.toLowerCase() || '';

    const table = document.getElementById('departmentsTable');
    if (!table) return;

    const rows = table.querySelectorAll('tbody tr');
    let visibleCount = 0;

    rows.forEach(row => {
        let show = true;

        // Branch filter
        if (branchFilter) {
            const rowBranchId = row.getAttribute('data-branch-id');
            if (rowBranchId !== branchFilter) {
                show = false;
            }
        }

        // Status filter
        if (statusFilter && show) {
            const rowStatus = row.getAttribute('data-status');
            if (rowStatus !== statusFilter) {
                show = false;
            }
        }

        // User count filter
        if (userCountFilter && show) {
            const userCount = parseInt(row.getAttribute('data-user-count'));

            if (userCountFilter === '0' && userCount !== 0) {
                show = false;
            } else if (userCountFilter === '1-5' && (userCount < 1 || userCount > 5)) {
                show = false;
            } else if (userCountFilter === '6-10' && (userCount < 6 || userCount > 10)) {
                show = false;
            } else if (userCountFilter === '11-20' && (userCount < 11 || userCount > 20)) {
                show = false;
            } else if (userCountFilter === '20+' && userCount < 20) {
                show = false;
            }
        }

        // Search filter
        if (searchText && show) {
            const deptName = row.getAttribute('data-department-name') || '';
            if (!deptName.includes(searchText)) {
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
    const table = document.getElementById('departmentsTable');
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
                    <p class="mt-2 mb-0 text-muted">No departments match your filters</p>
                </td>
            `;
            tbody.appendChild(noResultsRow);
        }
        noResultsRow.style.display = '';
    } else if (noResultsRow) {
        noResultsRow.style.display = 'none';
    }
}

// View department details
function viewDepartment(departmentId) {
    window.location.href = `/Departments/Details/${departmentId}`;
}

// Edit department
function editDepartment(departmentId) {
    window.location.href = `/Departments/Edit/${departmentId}`;
}

// Confirm delete department
function confirmDelete(departmentId, departmentName) {
    if (confirm(`Are you sure you want to delete department "${departmentName}"?\n\nThis will deactivate the department. Users assigned to this department will not be affected.`)) {
        deleteDepartment(departmentId);
    }
}

// Delete department
async function deleteDepartment(departmentId) {
    try {
        // Get the anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const formData = new FormData();
        formData.append('id', departmentId);
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        const response = await fetch(`/Departments/Delete/${departmentId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        });

        if (response.ok) {
            showToast('Department deactivated successfully', 'success');
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast('Failed to deactivate department', 'error');
        }
    } catch (error) {
        console.error('Error deactivating department:', error);
        showToast('An error occurred while deactivating department', 'error');
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
    const createForm = document.getElementById('createDepartmentForm');
    const editForm = document.getElementById('editDepartmentForm');

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
    const departmentsTable = document.getElementById('departmentsTable');
    if (departmentsTable) {
        populateBranchFilter();

        // Add event listeners to filters
        const branchFilter = document.getElementById('branchFilter');
        const statusFilter = document.getElementById('statusFilter');
        const userCountFilter = document.getElementById('userCountFilter');
        const searchDepartment = document.getElementById('searchDepartment');

        if (branchFilter) branchFilter.addEventListener('change', filterDepartments);
        if (statusFilter) statusFilter.addEventListener('change', filterDepartments);
        if (userCountFilter) userCountFilter.addEventListener('change', filterDepartments);
        if (searchDepartment) searchDepartment.addEventListener('input', filterDepartments);
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
