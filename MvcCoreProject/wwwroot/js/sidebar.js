// Sidebar JavaScript

document.addEventListener('DOMContentLoaded', function () {
    initializeSidebar();
    highlightActivePage();
});

// Initialize sidebar functionality
function initializeSidebar() {
    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');

    // Toggle sidebar with mobile menu button
    if (mobileMenuBtn) {
        mobileMenuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleSidebar();
        });
    }

    // Toggle sidebar on mobile (inside sidebar button)
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function (e) {
            e.stopPropagation();
            toggleSidebar();
        });
    }

    // Close sidebar when clicking overlay
    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', closeSidebar);
    }

    // Handle window resize
    let resizeTimer;
    window.addEventListener('resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () {
            if (window.innerWidth > 1024) {
                closeSidebar();
            }
        }, 250);
    });

    // Load sidebar state from localStorage
    loadSidebarState();
}

// Toggle sidebar
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');

    if (window.innerWidth <= 1024) {
        // Mobile: slide in/out
        sidebar.classList.toggle('open');
        overlay.classList.toggle('active');
    } else {
        // Desktop: collapse/expand
        sidebar.classList.toggle('collapsed');
        saveSidebarState();
    }
}

// Close sidebar
function closeSidebar() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');

    sidebar.classList.remove('open');
    overlay.classList.remove('active');
}

// Toggle submenu
function toggleSubmenu(event, element) {
    event.preventDefault();
    const parent = element.closest('.nav-item');
    const wasOpen = parent.classList.contains('open');

    // Close all other submenus
    document.querySelectorAll('.nav-item.has-submenu').forEach(item => {
        if (item !== parent) {
            item.classList.remove('open');
        }
    });

    // Toggle current submenu
    parent.classList.toggle('open');
}

// Highlight active page
function highlightActivePage() {
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.nav-link, .submenu a');

    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href && currentPath.includes(href.toLowerCase())) {
            link.classList.add('active');

            // If it's a submenu item, open its parent
            const submenu = link.closest('.submenu');
            if (submenu) {
                const parentItem = submenu.closest('.nav-item');
                if (parentItem) {
                    parentItem.classList.add('open');
                }
            }
        }
    });
}

// Save sidebar state to localStorage
function saveSidebarState() {
    const sidebar = document.getElementById('sidebar');
    const isCollapsed = sidebar.classList.contains('collapsed');
    localStorage.setItem('sidebarCollapsed', isCollapsed);
}

// Load sidebar state from localStorage
function loadSidebarState() {
    const sidebar = document.getElementById('sidebar');
    const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';

    if (isCollapsed && window.innerWidth > 1024) {
        sidebar.classList.add('collapsed');
    }
}

// Add tooltips to navigation links
function addTooltips() {
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        const text = link.querySelector('.nav-text');
        if (text) {
            link.setAttribute('data-tooltip', text.textContent.trim());
        }
    });
}

// Initialize tooltips
addTooltips();