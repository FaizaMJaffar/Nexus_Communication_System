/* Nexus shared shell behaviour: mobile menu, collapse rail, top-bar search */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var body = document.body;

        // restore collapsed state (wrapped: storage can be blocked)
        try { if (localStorage.getItem('nx-collapsed') === '1') body.classList.add('nx-collapsed'); } catch (e) { }

        var menuBtn = document.getElementById('nxMenuBtn');
        if (menuBtn) {
            menuBtn.addEventListener('click', function () { body.classList.toggle('nx-nav-open'); });
        }
        var backdrop = document.getElementById('nxBackdrop');
        if (backdrop) {
            backdrop.addEventListener('click', function () { body.classList.remove('nx-nav-open'); });
        }

        var collapseBtn = document.getElementById('nxCollapse');
        if (collapseBtn) {
            collapseBtn.addEventListener('click', function () {
                var on = body.classList.toggle('nx-collapsed');
                try { localStorage.setItem('nx-collapsed', on ? '1' : '0'); } catch (e) { }
                var ico = collapseBtn.querySelector('.material-symbols-outlined');
                if (ico) ico.textContent = on ? 'keyboard_double_arrow_right' : 'keyboard_double_arrow_left';
            });
        }

        // top-bar search: filters rows of the tables on the current page
        var search = document.querySelector('.nx-search input');
        if (search) {
            search.addEventListener('input', function () {
                var term = search.value.trim().toLowerCase();
                document.querySelectorAll('.nx-content table').forEach(function (table) {
                    table.querySelectorAll('tr').forEach(function (row) {
                        if (row.querySelector('th')) return;
                        row.style.display = (!term || row.textContent.toLowerCase().indexOf(term) !== -1) ? '' : 'none';
                    });
                });
            });
        }

        // password show/hide on the shared login page
        document.querySelectorAll('[data-nx-eye]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var input = document.getElementById(btn.getAttribute('data-nx-eye'));
                if (!input) return;
                var show = input.type === 'password';
                input.type = show ? 'text' : 'password';
                var ico = btn.querySelector('.material-symbols-outlined');
                if (ico) ico.textContent = show ? 'visibility_off' : 'visibility';
            });
        });
    });
})();
