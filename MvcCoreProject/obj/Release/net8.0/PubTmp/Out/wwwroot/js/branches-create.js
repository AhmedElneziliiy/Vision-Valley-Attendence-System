let departmentCount = 1;

function addDepartmentInput() {
    const container = document.getElementById('departmentsContainer');
    const newInput = document.createElement('div');
    newInput.className = 'department-input-group mb-2';
    newInput.innerHTML = `
        <div class="input-with-icon">
            <i class="bi bi-diagram-3"></i>
            <input type="text" name="DepartmentNames[${departmentCount}]" class="form-control" placeholder="Department name (optional)" />
            <button type="button" class="btn btn-sm btn-danger" onclick="this.closest('.department-input-group').remove()">
                <i class="bi bi-trash"></i>
            </button>
        </div>
    `;
    container.appendChild(newInput);
    departmentCount++;
}
