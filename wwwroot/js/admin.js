(function () {
    'use strict';

    // mobile sidebar toggle
    var menuBtn = document.getElementById('menuBtn');
    var sidebar = document.getElementById('sidebar');
    if (menuBtn && sidebar) {
        menuBtn.addEventListener('click', function () {
            sidebar.classList.toggle('open');
        });
        document.addEventListener('click', function (e) {
            if (window.innerWidth <= 900 && sidebar.classList.contains('open') &&
                !sidebar.contains(e.target) && e.target !== menuBtn) {
                sidebar.classList.remove('open');
            }
        });
    }

    // confirm before delete
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (!window.confirm(form.getAttribute('data-confirm'))) {
                e.preventDefault();
            }
        });
    });

    // auto hide success messages
    document.querySelectorAll('[data-autohide]').forEach(function (el) {
        setTimeout(function () {
            el.style.transition = 'opacity .4s';
            el.style.opacity = '0';
            setTimeout(function () { el.remove(); }, 450);
        }, 4000);
    });
})();
