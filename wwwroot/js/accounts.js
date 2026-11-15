

document.addEventListener("DOMContentLoaded", function () {
    checkAmountFields();
    confirmPayment();
    showChargePreview();
    addTableSearch();
    markOverdueRows();
});


// 1. Form check: amounts must be valid before the form is sent
function checkAmountFields() {
    var forms = document.querySelectorAll("form");

    forms.forEach(function (form) {
        form.addEventListener("submit", function (e) {

            // these fields must be greater than 0
            var mustBePositive = ["amount", "amountPaid", "unitPrice"];

            for (var i = 0; i < mustBePositive.length; i++) {
                var input = form.querySelector('input[name="' + mustBePositive[i] + '"]');

                if (input && Number(input.value) <= 0) {
                    alert("Please enter an amount greater than 0.");
                    input.focus();
                    e.preventDefault();
                    return;
                }
            }

            // these fields can be 0 but not negative
            var notNegative = ["securityDeposit", "depositAmount"];

            for (var j = 0; j < notNegative.length; j++) {
                var field = form.querySelector('input[name="' + notNegative[j] + '"]');

                if (field && Number(field.value) < 0) {
                    alert("Deposit cannot be negative.");
                    field.focus();
                    e.preventDefault();
                    return;
                }
            }
        });
    });
}


// 2. Ask "are you sure?" before saving a payment
function confirmPayment() {
    var amountInput = document.querySelector('input[name="amountPaid"]');
    if (!amountInput) {
        return;
    }

    var form = amountInput.closest("form");

    form.addEventListener("submit", function (e) {
        // if the check above already stopped the form, do nothing here
        if (e.defaultPrevented) {
            return;
        }

        var billId = form.querySelector('input[name="billId"]').value;
        var ok = confirm("Save payment of " + amountInput.value + " for Bill " + billId + "?");

        if (!ok) {
            e.preventDefault();
        }
    });
}


// 3. Charges page: show Quantity x Unit Price live
function showChargePreview() {
    var quantity = document.querySelector('input[name="quantity"]');
    var unitPrice = document.querySelector('input[name="unitPrice"]');

    if (!quantity || !unitPrice) {
        return;
    }

    var preview = document.createElement("p");
    preview.style.fontWeight = "600";
    preview.textContent = "Charge amount: 0.00";
    unitPrice.parentElement.appendChild(preview);

    function update() {
        var total = Number(quantity.value) * Number(unitPrice.value);
        preview.textContent = "Charge amount: " + total.toFixed(2);
    }

    quantity.addEventListener("input", update);
    unitPrice.addEventListener("input", update);
    update();
}


// 4. Search box above every table (Payments, Bill History, Due Payments etc.)
function addTableSearch() {
    var tables = document.querySelectorAll("table.table");

    tables.forEach(function (table) {
        var rows = table.querySelectorAll("tr");

        // skip small tables (like the old dashboard table)
        if (rows.length < 4) {
            return;
        }

        var search = document.createElement("input");
        search.type = "text";
        search.placeholder = "Search in table...";
        search.className = "form-control";
        search.style.maxWidth = "320px";
        search.style.marginBottom = "12px";

        table.parentNode.insertBefore(search, table);

        search.addEventListener("keyup", function () {
            var text = search.value.toLowerCase();

            // start from 1 so the header row is never hidden
            for (var i = 1; i < rows.length; i++) {
                var rowText = rows[i].textContent.toLowerCase();
                rows[i].style.display = rowText.indexOf(text) > -1 ? "" : "none";
            }
        });
    });
}


// 5. Due Payments page: colour rows red if the due date has passed
function markOverdueRows() {
    var tables = document.querySelectorAll("table.table");

    tables.forEach(function (table) {
        var headers = table.querySelectorAll("th");
        var dueIndex = -1;

        for (var i = 0; i < headers.length; i++) {
            if (headers[i].textContent.trim() === "Due Date") {
                dueIndex = i;
            }
        }

        if (dueIndex === -1) {
            return;
        }

        var today = new Date();
        today.setHours(0, 0, 0, 0);

        var rows = table.querySelectorAll("tr");

        for (var r = 1; r < rows.length; r++) {
            var cells = rows[r].querySelectorAll("td");
            if (cells.length <= dueIndex) {
                continue;
            }

            // date is written as dd-MM-yyyy
            var parts = cells[dueIndex].textContent.trim().split("-");
            var dueDate = new Date(parts[2], parts[1] - 1, parts[0]);

            if (dueDate < today) {
                cells.forEach(function (cell) {
                    cell.style.backgroundColor = "#FEF2F2";
                });
            }
        }
    });
}