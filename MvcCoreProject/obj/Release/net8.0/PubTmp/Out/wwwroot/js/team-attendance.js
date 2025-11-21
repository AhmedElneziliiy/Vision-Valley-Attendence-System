// Search functionality
document.getElementById('searchInput')?.addEventListener('input', function(e) {
    const searchTerm = e.target.value.toLowerCase();
    const rows = document.querySelectorAll('#teamTable tbody tr');
    let visibleCount = 0;

    rows.forEach(row => {
        const userName = row.dataset.userName;
        const department = row.dataset.department;
        const matches = userName.includes(searchTerm) || department.includes(searchTerm);

        if (matches) {
            row.style.display = '';
            visibleCount++;
        } else {
            row.style.display = 'none';
        }
    });

    const noResults = document.getElementById('noResults');
    const table = document.getElementById('teamTable');
    if (visibleCount === 0) {
        table.style.display = 'none';
        noResults.classList.remove('d-none');
    } else {
        table.style.display = '';
        noResults.classList.add('d-none');
    }
});

function setDate(period) {
    const dateInput = document.getElementById('date');
    const today = new Date();

    if (period === 'today') {
        dateInput.value = today.toISOString().split('T')[0];
    } else if (period === 'yesterday') {
        today.setDate(today.getDate() - 1);
        dateInput.value = today.toISOString().split('T')[0];
    }
}

function setRangeFilter(period) {
    const today = new Date();
    const startDateInput = document.getElementById('startDate');
    const endDateInput = document.getElementById('endDate');

    let startDate = new Date();

    switch(period) {
        case 'week':
            startDate = new Date(today.setDate(today.getDate() - today.getDay()));
            break;
        case 'month':
            startDate = new Date(today.getFullYear(), today.getMonth(), 1);
            break;
        case '7days':
            startDate = new Date(today.setDate(today.getDate() - 7));
            break;
    }

    startDateInput.value = startDate.toISOString().split('T')[0];
    endDateInput.value = new Date().toISOString().split('T')[0];
}
