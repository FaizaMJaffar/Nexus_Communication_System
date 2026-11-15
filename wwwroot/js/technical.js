(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {

        document.querySelectorAll(".tx-alert-success").forEach(function (alertBox) {
            setTimeout(function () {
                alertBox.style.transition = "opacity 0.4s";
                alertBox.style.opacity = "0";
                setTimeout(function () { alertBox.remove(); }, 400);
            }, 4000);
        });

        var searchInput = document.querySelector(".tx-search input");
        if (searchInput) {
            searchInput.addEventListener("input", function () {
                var term = searchInput.value.trim().toLowerCase();
                document.querySelectorAll(".tx-table tbody tr").forEach(function (row) {
                    row.style.display = row.textContent.toLowerCase().includes(term) ? "" : "none";
                });
            });
        }

        document.querySelectorAll("button.tx-btn-danger[type='submit']").forEach(function (btn) {
            btn.addEventListener("click", function (e) {
                var label = btn.textContent.trim();
                if (!confirm("Are you sure you want to: " + label + "?")) {
                    e.preventDefault();
                }
            });
        });

        document.querySelectorAll("form").forEach(function (form) {
            form.addEventListener("submit", function (e) {
                if (form.dataset.submitting === "true") {
                    e.preventDefault();
                    return;
                }
                form.dataset.submitting = "true";
            });
        });

      
        var mac = document.getElementById("MacAddress");
        if (mac) {
            mac.addEventListener("blur", function () {
                mac.value = mac.value.trim().toUpperCase();
            });
        }
    });
})();