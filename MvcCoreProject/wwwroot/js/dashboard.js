// Dashboard JavaScript

// Initialize Monthly Check-ins Chart
function initMonthlyCheckInsChart(data) {
    const ctx = document.getElementById('monthlyCheckInsChart');
    if (!ctx) return;

    const labels = data.map(item => item.month);
    const values = data.map(item => item.checkIns);

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Check-ins',
                data: values,
                borderColor: '#667eea',
                backgroundColor: 'rgba(102, 126, 234, 0.1)',
                borderWidth: 3,
                fill: true,
                tension: 0.4,
                pointRadius: 5,
                pointHoverRadius: 8,
                pointBackgroundColor: '#667eea',
                pointBorderColor: '#fff',
                pointBorderWidth: 2,
                pointHoverBackgroundColor: '#667eea',
                pointHoverBorderColor: '#fff',
                pointHoverBorderWidth: 3
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    titleFont: {
                        size: 14,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    },
                    borderColor: '#667eea',
                    borderWidth: 1,
                    displayColors: false,
                    callbacks: {
                        label: function (context) {
                            return 'Check-ins: ' + context.parsed.y.toLocaleString();
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        font: {
                            size: 12
                        },
                        color: '#718096'
                    }
                },
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)',
                        drawBorder: false
                    },
                    ticks: {
                        font: {
                            size: 12
                        },
                        color: '#718096',
                        callback: function (value) {
                            return value.toLocaleString();
                        }
                    }
                }
            },
            interaction: {
                intersect: false,
                mode: 'index'
            },
            animation: {
                duration: 1000,
                easing: 'easeInOutQuart'
            }
        }
    });
}

// Refresh Dashboard
async function refreshDashboard() {
    const btn = document.getElementById('refreshBtn');
    const icon = btn.querySelector('i');

    // Add loading state
    btn.disabled = true;
    btn.classList.add('loading');

    try {
        const response = await fetch('/Dashboard/RefreshStats');
        const result = await response.json();

        if (result.success) {
            // Show success message
            showToast('Dashboard refreshed successfully!', 'success');

            // Reload page after short delay
            setTimeout(() => {
                location.reload();
            }, 500);
        } else {
            showToast('Failed to refresh dashboard', 'error');
        }
    } catch (error) {
        console.error('Error refreshing dashboard:', error);
        showToast('An error occurred while refreshing', 'error');
    } finally {
        // Remove loading state
        setTimeout(() => {
            btn.disabled = false;
            btn.classList.remove('loading');
        }, 500);
    }
}

// Show Toast Notification
function showToast(message, type = 'info') {
    // Remove existing toasts
    const existingToast = document.querySelector('.toast-notification');
    if (existingToast) {
        existingToast.remove();
    }

    // Create toast element
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.innerHTML = `
        <div class="toast-content">
            <i class="bi bi-${type === 'success' ? 'check-circle-fill' : 'exclamation-circle-fill'}"></i>
            <span>${message}</span>
        </div>
    `;

    // Add to body
    document.body.appendChild(toast);

    // Show toast
    setTimeout(() => toast.classList.add('show'), 100);

    // Remove toast after 3 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Animate numbers on page load
function animateNumbers() {
    const statValues = document.querySelectorAll('.stat-card-value');

    statValues.forEach(element => {
        const target = parseInt(element.textContent);
        const duration = 1000;
        const increment = target / (duration / 16);
        let current = 0;

        const timer = setInterval(() => {
            current += increment;
            if (current >= target) {
                element.textContent = target;
                clearInterval(timer);
            } else {
                element.textContent = Math.floor(current);
            }
        }, 16);
    });
}

// Animate progress bars
function animateProgressBars() {
    const progressBars = document.querySelectorAll('.department-progress-bar');

    progressBars.forEach(bar => {
        const targetWidth = bar.getAttribute('data-percentage') + '%';
        bar.style.width = '0%';

        setTimeout(() => {
            bar.style.width = targetWidth;
        }, 100);
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    animateNumbers();
    animateProgressBars();

    // Auto-refresh every 5 minutes
    setInterval(() => {
        console.log('Auto-refreshing dashboard...');
        refreshDashboard();
    }, 300000); // 5 minutes
});