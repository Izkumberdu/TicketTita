document.addEventListener("DOMContentLoaded", function () {
    const filterBySelect = document.getElementById("filterBy");
    const filterValueSelect = document.getElementById("filterValue");

    const priorityOptions = JSON.parse(document.getElementById("priorityOptions").value);
    const statusOptions = JSON.parse(document.getElementById("statusOptions").value);
    const categoryOptions = JSON.parse(document.getElementById("categoryOptions").value);
    const userOptions = JSON.parse(document.getElementById("userOptions").value);

    function populateOptions(options) {
        filterValueSelect.innerHTML = "";
        options.forEach(option => {
            const opt = document.createElement("option");
            opt.value = option;
            opt.text = option;
            if (option === document.getElementById("filterValueHidden").value) {
                opt.selected = true;
            }
            filterValueSelect.add(opt);
        });
    }

    function updateFilterValues() {
        const filterBy = filterBySelect.value;
        if (filterBy === "") {
            filterValueSelect.style.display = "none";
        } else {
            filterValueSelect.style.display = "inline-block";
            switch (filterBy) {
                case "priority":
                    populateOptions(priorityOptions);
                    break;
                case "status":
                    populateOptions(statusOptions);
                    break;
                case "category":
                    populateOptions(categoryOptions);
                    break;
                case "user":
                    populateOptions(userOptions);
                    break;
                default:
                    filterValueSelect.innerHTML = "";
                    break;
            }
        }
    }

    filterBySelect.addEventListener("change", updateFilterValues);

    updateFilterValues();
});
