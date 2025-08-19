window.addEventListener('DOMContentLoaded', event => {

    // Navbar shrink function
    var navbarShrink = function () {
        const navbarCollapsible = document.body.querySelector('#mainNav');
        if (!navbarCollapsible) {
            return;
        }
        if (window.scrollY === 0) {
            navbarCollapsible.classList.remove('navbar-shrink')
        } else {
            navbarCollapsible.classList.add('navbar-shrink')
        }

    };

    // Shrink the navbar 
    navbarShrink();

    // Shrink the navbar when page is scrolled
    document.addEventListener('scroll', navbarShrink);

    const navbarCollapse = document.getElementById('navbarResponsive');
    const customDropdownToggles = document.querySelectorAll('.js-custom-dropdown-toggle');
    const customNavbarToggler = document.querySelector('.js-custom-navbar-toggler');

    // Helper function to close all dropdown menus
    function closeAllDropdowns(excludeToggle = null) {
        customDropdownToggles.forEach(toggle => {
            if (toggle !== excludeToggle) {
                toggle.setAttribute('aria-expanded', 'false');
                const menuId = toggle.getAttribute('id');
                const menu = document.querySelector(`[aria-labelledby="${menuId}"]`);
                if (menu) {
                    menu.classList.remove('show');
                }
            }
        });
    }

    // Event listener for the main navbar toggler (hamburger button)
    if (customNavbarToggler && navbarCollapse) {
        customNavbarToggler.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();

            const isExpanded = customNavbarToggler.getAttribute('aria-expanded') === 'true';

            // Toggle 'show' class on the collapsible navbar content
            navbarCollapse.classList.toggle('show');
            // Update aria-expanded attribute on the toggler button
            customNavbarToggler.setAttribute('aria-expanded', !isExpanded);

            // If the main navbar is closed, ensure all nested dropdowns are also closed
            if (!navbarCollapse.classList.contains('show')) {
                closeAllDropdowns(); // Call the helper to close all dropdowns
            }
        });
    }

    // Event listener for custom dropdown toggles (e.g., Account, NavigationGroup)
    customDropdownToggles.forEach(toggle => {
        toggle.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation(); // Crucial to prevent bubbling to main navbar toggler

            const isExpanded = toggle.getAttribute('aria-expanded') === 'true';
            const menuId = toggle.getAttribute('id');
            const menu = document.querySelector(`[aria-labelledby="${menuId}"]`);

            if (!menu) {
                console.warn(`Dropdown menu not found for toggle with aria-labelledby="${menuId}"`);
                return;
            }

            closeAllDropdowns(toggle); // Close other dropdowns before opening this one

            // Toggle current dropdown:
            toggle.setAttribute('aria-expanded', !isExpanded);
            menu.classList.toggle('show');
        });
    });

    // Global 'document' click listener to close menus when clicking outside
    document.addEventListener('click', function (event) {
        const isClickInsideNavbar = navbarCollapse.contains(event.target) ||
            (customNavbarToggler && customNavbarToggler.contains(event.target));

        let isClickInsideDropdown = false;
        customDropdownToggles.forEach(toggle => {
            const menuId = toggle.getAttribute('id');
            const menu = document.querySelector(`[aria-labelledby="${menuId}"]`);
            if (menu && (menu.contains(event.target) || toggle.contains(event.target))) {
                isClickInsideDropdown = true;
            }
        });

        if (!isClickInsideNavbar && !isClickInsideDropdown) {
            // Close main navbar if it's open
            if (navbarCollapse.classList.contains('show')) {
                navbarCollapse.classList.remove('show');
                if (customNavbarToggler) {
                    customNavbarToggler.setAttribute('aria-expanded', 'false');
                }
            }
            // Close all dropdowns
            closeAllDropdowns();
        }
    });
});
