// Update current time every second
function updateTime() {
    const now = new Date();
    const hours = String(now.getUTCHours()).padStart(2, '0');
    const minutes = String(now.getUTCMinutes()).padStart(2, '0');
    const seconds = String(now.getUTCSeconds()).padStart(2, '0');
    document.getElementById('currentTime').textContent = `${hours}:${minutes}:${seconds}`;
}

// Update time immediately and then every second
updateTime();
setInterval(updateTime, 1000);

// Show confirmation before check out
document.getElementById('checkOutBtn')?.addEventListener('click', function(e) {
    if (!this.disabled) {
        if (!confirm('Are you sure you want to check out?')) {
            e.preventDefault();
        }
    }
});
