/* Nexus Communication System - Customer Portal scripts */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        // Mobile menu toggle
        var toggle = document.getElementById('navToggle');
        var links = document.getElementById('navLinks');
        if (toggle && links) {
            toggle.addEventListener('click', function () {
                links.classList.toggle('open');
            });
        }

        // Auto-hide success alerts
        document.querySelectorAll('[data-autohide]').forEach(function (el) {
            setTimeout(function () {
                el.style.opacity = '0';
                setTimeout(function () { el.style.display = 'none'; }, 500);
            }, 4500);
        });

        // Password show / hide
        document.querySelectorAll('.pw-toggle').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var input = btn.parentElement.querySelector('input');
                if (!input) return;
                var show = input.type === 'password';
                input.type = show ? 'text' : 'password';
                btn.textContent = show ? '🙈' : '👁';
            });
        });

        // Confirmation dialog for elements with data-confirm
        document.querySelectorAll('[data-confirm]').forEach(function (el) {
            var target = el.tagName === 'FORM' ? el : el.closest('form');
            if (!target) return;
            target.addEventListener('submit', function (e) {
                if (!confirm(el.getAttribute('data-confirm'))) e.preventDefault();
            });
        });

        // Print button
        document.querySelectorAll('[data-print]').forEach(function (btn) {
            btn.addEventListener('click', function () { window.print(); });
        });

        // Select Plan page: live summary
        var summary = document.getElementById('planSummary');
        var planInputs = document.querySelectorAll('input[name="PlanId"]');
        if (summary && planInputs.length) {
            var fee = parseFloat(summary.getAttribute('data-fee')) || 0;
            var taxPct = parseFloat(summary.getAttribute('data-tax')) || 0;
            var fmt = function (n) { return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); };
            var update = function () {
                var sel = document.querySelector('input[name="PlanId"]:checked');
                if (!sel) return;
                var price = parseFloat(sel.getAttribute('data-price')) || 0;
                document.getElementById('sumPlan').textContent = sel.getAttribute('data-name');
                document.getElementById('sumMonthly').textContent = fmt(price);
                var tax = Math.round((price + fee) * taxPct) / 100;   // 12.24 % on plan + deposit
                document.getElementById('sumTax').textContent = fmt(tax);
                document.getElementById('sumTotal').textContent = fmt(price + fee + tax);
            };
            planInputs.forEach(function (i) { i.addEventListener('change', update); });
            update();
        }

        // Contact number: allow only valid characters
        document.querySelectorAll('input[type=tel]').forEach(function (el) {
            el.addEventListener('input', function () {
                el.value = el.value.replace(/[^0-9+\-\s]/g, '');
            });
        });
    });
})();
