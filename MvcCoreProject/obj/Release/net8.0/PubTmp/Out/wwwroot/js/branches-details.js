function showAddDepartmentModal() {
    const modal = new bootstrap.Modal(document.getElementById('addDepartmentModal'));
    modal.show();
}

async function addDepartment() {
    const branchId = document.getElementById('branchId').value;
    const departmentName = document.getElementById('departmentName').value;

    if (!departmentName.trim()) {
        alert('Please enter a department name');
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
                  document.querySelector('[name="__RequestVerificationToken"]')?.value;

    const formData = new FormData();
    formData.append('branchId', branchId);
    formData.append('departmentName', departmentName);
    if (token) formData.append('__RequestVerificationToken', token);

    try {
        const response = await fetch('/Branches/AddDepartment', {
            method: 'POST',
            headers: token ? { 'RequestVerificationToken': token } : {},
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            location.reload();
        } else {
            alert(result.message || 'Failed to add department');
        }
    } catch (error) {
        console.error('Error adding department:', error);
        alert('An error occurred while adding department');
    }
}

async function removeDepartment(departmentId, departmentName) {
    if (!confirm(`Are you sure you want to remove "${departmentName}"?\n\nThis will deactivate the department.`)) {
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
                  document.querySelector('[name="__RequestVerificationToken"]')?.value;

    const formData = new FormData();
    formData.append('departmentId', departmentId);
    if (token) formData.append('__RequestVerificationToken', token);

    try {
        const response = await fetch('/Branches/RemoveDepartment', {
            method: 'POST',
            headers: token ? { 'RequestVerificationToken': token } : {},
            body: formData
        });

        const result = await response.json();

        if (result.success) {
            location.reload();
        } else {
            alert(result.message || 'Failed to remove department');
        }
    } catch (error) {
        console.error('Error removing department:', error);
        alert('An error occurred while removing department');
    }
}
