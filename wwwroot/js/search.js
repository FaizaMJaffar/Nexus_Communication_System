// Search & Reports module scripts
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {

        // 1. From date must not be after To date
        document.querySelectorAll("form[data-search]").forEach(function (form) {
            var from = form.querySelector("[name='fromDate'], [name='FromDate']");
            var to = form.querySelector("[name='toDate'], [name='ToDate']");
            if (!from || !to) return;
            form.addEventListener("submit", function (e) {
                if (from.value && to.value && from.value > to.value) {
                    e.preventDefault();
                    alert("The 'From' date must be before the 'To' date.");
                    from.focus();
                }
            });
        });

        // 2. Keep URLs clean: do not send empty fields
        document.querySelectorAll("form[data-search][data-clean='true']").forEach(function (form) {
            form.addEventListener("submit", function () {
                form.querySelectorAll("input, select").forEach(function (el) {
                    if (!el.value) el.disabled = true;
                });
            });
        });

        // 3. Clear / Reset: reload the page without filters
        document.querySelectorAll("[data-action='clear-search']").forEach(function (btn) {
            btn.addEventListener("click", function (e) {
                e.preventDefault();
                window.location.href = window.location.pathname;
            });
        });

        // 4. Advanced search: status list and amount fields depend on the category
        var category = document.getElementById("Category");
        if (category) {
            var statusSelect = document.getElementById("advStatus");
            var amountGroup = document.querySelectorAll(".amount-field");
            var statusOptions = {
                Orders: ["Processing", "Completed", "Rejected"],
                Connections: ["Pending", "Active", "Temporarily Inactive", "Permanently Inactive"],
                Bills: ["Paid", "Partial", "Unpaid"],
                Customers: []
            };

            function refreshFields() {
                var cat = category.value;
                var current = statusSelect ? (statusSelect.getAttribute("data-selected") || statusSelect.value) : "";
                if (statusSelect) {
                    var opts = statusOptions[cat] || [];
                    statusSelect.innerHTML = '<option value="">All</option>';
                    opts.forEach(function (o) {
                        var op = document.createElement("option");
                        op.value = o; op.textContent = o;
                        if (o === current) op.selected = true;
                        statusSelect.appendChild(op);
                    });
                    statusSelect.closest(".form-group").classList.toggle("hidden", opts.length === 0);
                }
                var showAmount = (cat === "Orders" || cat === "Bills");
                amountGroup.forEach(function (g) { g.classList.toggle("hidden", !showAmount); });
            }

            category.addEventListener("change", function () {
                if (statusSelect) statusSelect.setAttribute("data-selected", "");
                refreshFields();
            });
            refreshFields();
        }

        // 5. Quick filter inside a report table
        var quick = document.getElementById("quickFilter");
        if (quick) {
            quick.addEventListener("input", function () {
                var term = quick.value.toLowerCase();
                document.querySelectorAll(".nx-content table tbody tr").forEach(function (row) {
                    row.style.display = row.textContent.toLowerCase().indexOf(term) > -1 ? "" : "none";
                });
            });
        }

        // 6. Print
        document.querySelectorAll("[data-action='print']").forEach(function (btn) {
            btn.addEventListener("click", function (e) { e.preventDefault(); window.print(); });
        });

        // 7. Export the visible table to CSV
        document.querySelectorAll("[data-action='export-csv']").forEach(function (btn) {
            btn.addEventListener("click", function (e) {
                e.preventDefault();
                var table = document.querySelector(".nx-content table");
                if (!table) return;
                var rows = [];
                table.querySelectorAll("tr").forEach(function (tr) {
                    if (tr.style.display === "none") return;
                    var cells = [];
                    tr.querySelectorAll("th, td").forEach(function (c) {
                        cells.push('"' + c.innerText.replace(/"/g, '""').trim() + '"');
                    });
                    rows.push(cells.join(","));
                });
                var blob = new Blob(["\uFEFF" + rows.join("\n")], { type: "text/csv;charset=utf-8;" });
                var a = document.createElement("a");
                a.href = URL.createObjectURL(blob);
                a.download = (btn.getAttribute("data-filename") || "report") + ".csv";
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
            });
        });
    });
})();
