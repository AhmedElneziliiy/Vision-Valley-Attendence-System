function setQuickFilter(period) {
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
        case 'lastMonth':
            startDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
            const endOfLastMonth = new Date(today.getFullYear(), today.getMonth(), 0);
            endDateInput.value = endOfLastMonth.toISOString().split('T')[0];
            startDateInput.value = startDate.toISOString().split('T')[0];
            return;
        case 'quarter':
            const quarter = Math.floor(today.getMonth() / 3);
            startDate = new Date(today.getFullYear(), quarter * 3, 1);
            break;
    }

    startDateInput.value = startDate.toISOString().split('T')[0];
    endDateInput.value = new Date().toISOString().split('T')[0];
}

// Attendance Distribution Chart
const attendanceCtx = document.getElementById('attendanceChart')?.getContext('2d');
if (attendanceCtx) {
    new Chart(attendanceCtx, {
        type: 'doughnut',
        data: {
            labels: ['Present', 'Absent'],
            datasets: [{
                data: [window.chartData.presentDays, window.chartData.absentDays],
                backgroundColor: ['#198754', '#dc3545'],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom'
                }
            }
        }
    });
}

// Daily Hours Trend Chart
const hoursCtx = document.getElementById('hoursChart')?.getContext('2d');
if (hoursCtx) {
    const attendances = window.chartData.attendances;

    new Chart(hoursCtx, {
        type: 'line',
        data: {
            labels: attendances.map(a => new Date(a.Date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })),
            datasets: [{
                label: 'Hours Worked',
                data: attendances.map(a => (a.Duration / 60.0).toFixed(2)),
                borderColor: '#0d6efd',
                backgroundColor: 'rgba(13, 110, 253, 0.1)',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Hours'
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });
}
