(function() {
    'use strict';
    
    function toggleDropdown(element) {
        const menu = element.nextElementSibling || element.parentElement.querySelector('.dropdown-menu');
        
        if (!menu) return;
        
        const isShown = menu.classList.contains('show');
        
        document.querySelectorAll('.dropdown-menu.show').forEach(function(m) {
            m.classList.remove('show');
        });
        
        document.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(function(btn) {
            btn.setAttribute('aria-expanded', 'false');
        });
        
        if (!isShown) {
            menu.classList.add('show');
            element.setAttribute('aria-expanded', 'true');
            
            const rect = menu.getBoundingClientRect();
            const viewportWidth = window.innerWidth;
            
            if (rect.right > viewportWidth) {
                menu.style.right = '0';
                menu.style.left = 'auto';
            }
            
            if (rect.left < 0) {
                menu.style.left = '0';
                menu.style.right = 'auto';
            }
        }
    }
    
    function handleClick(e) {
        const toggle = e.target.closest('[data-bs-toggle="dropdown"]');
        if (!toggle) {
            const clickedInside = e.target.closest('.dropdown-menu');
            if (!clickedInside) {
                document.querySelectorAll('.dropdown-menu.show').forEach(function(menu) {
                    menu.classList.remove('show');
                });
                document.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(function(btn) {
                    btn.setAttribute('aria-expanded', 'false');
                });
            }
            return;
        }
        
        e.preventDefault();
        e.stopPropagation();
        
        toggleDropdown(toggle);
    }
    
    function init() {
        document.addEventListener('click', handleClick, true);
        
        document.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(function(btn) {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                toggleDropdown(this);
            });
        });
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();

