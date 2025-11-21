// Users Management JavaScript

// Load departments based on selected branch
async function loadDepartments(branchId) {
    const departmentSelect = document.getElementById('departmentSelect');

    if (!branchId || branchId === '0') {
        departmentSelect.innerHTML = '<option value="0">— No departments available —</option>';
        return;
    }

    try {
        // Show loading
        departmentSelect.innerHTML = '<option value="">Loading...</option>';
        departmentSelect.disabled = true;

        const response = await fetch(`/Users/GetDepartments?branchId=${branchId}`);
        const result = await response.json();

        if (result.success && result.data && result.data.length > 0) {
            departmentSelect.innerHTML = result.data
                .map(dept => `<option value="${dept.id}">${dept.name}</option>`)
                .join('');
        } else {
            departmentSelect.innerHTML = '<option value="0">— No departments available —</option>';
        }
    } catch (error) {
        console.error('Error loading departments:', error);
        departmentSelect.innerHTML = '<option value="0">— Error loading departments —</option>';
        showToast('Failed to load departments', 'error');
    } finally {
        departmentSelect.disabled = false;
    }
}

// Toggle password visibility
function togglePasswordVisibility(inputId) {
    const input = document.getElementById(inputId);
    const icon = document.getElementById(`${inputId}-icon`);

    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('bi-eye');
        icon.classList.add('bi-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('bi-eye-slash');
        icon.classList.add('bi-eye');
    }
}

// View user details
function viewUser(userId) {
    window.location.href = `/Users/Details/${userId}`;
}

// Edit user
function editUser(userId) {
    window.location.href = `/Users/Edit/${userId}`;
}

// View attendance report for user
function viewAttendanceReport(userId) {
    window.location.href = `/Attendance/UserReport?userId=${userId}`;
}

// Confirm reset password
function confirmResetPassword(userId, userName) {
    if (confirm(`Are you sure you want to reset the password for "${userName}"?\n\nThe password will be reset to the default: Pass@123`)) {
        resetPassword(userId);
    }
}

// Reset user password
async function resetPassword(userId) {
    try {
        // Get the anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const formData = new FormData();
        formData.append('id', userId);
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        const response = await fetch(`/Users/ResetPassword/${userId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        });

        if (response.ok) {
            showToast('Password reset successfully! New password is: Pass@123', 'success');
            setTimeout(() => location.reload(), 3000);
        } else {
            showToast('Failed to reset password', 'error');
        }
    } catch (error) {
        console.error('Error resetting password:', error);
        showToast('An error occurred while resetting password', 'error');
    }
}

// Confirm delete user
function confirmDelete(userId, userName) {
    if (confirm(`Are you sure you want to delete user "${userName}"?\n\nThis will deactivate the user account. The user will no longer be able to log in.`)) {
        deleteUser(userId);
    }
}

// Delete user
async function deleteUser(userId) {
    try {
        // Get the anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        const formData = new FormData();
        formData.append('id', userId);
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        const response = await fetch(`/Users/Delete/${userId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        });

        if (response.ok) {
            showToast('User deactivated successfully', 'success');
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast('Failed to deactivate user', 'error');
        }
    } catch (error) {
        console.error('Error deactivating user:', error);
        showToast('An error occurred while deactivating user', 'error');
    }
}

// Export users
function exportUsers() {
    const form = document.getElementById('filterForm');
    const formData = new FormData(form);
    const params = new URLSearchParams(formData);

    window.location.href = `/Users/ExportUsers?${params.toString()}`;
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
    const createForm = document.getElementById('createUserForm');
    const editForm = document.getElementById('editUserForm');

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

    // Auto-dismiss alerts
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});