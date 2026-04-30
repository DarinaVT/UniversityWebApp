


(function() {
    'use strict';
    
    function init() {
        document.body.classList.add('loaded');
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            setTimeout(init, 100);
        });
    } else {
        setTimeout(init, 100);
    }
})();