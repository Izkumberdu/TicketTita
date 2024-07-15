var itemId;
function displayDeleteModal(id, name) {
    itemId = id;
    itemName = name;
    document.getElementById('deleteModalMessage').textContent = `Are you sure you want to delete ${itemName}?`;
    $('#deleteModal').modal('show');
}

$('#deleteModal').on('hidden.bs.modal', function () {
    toastr.info("Deletion cancelled, no changes were made");
});

$('#confirmDeleteBtn').on('click', function () {
    var deleteUrl = $('#deleteUrl').val();
    var baseUrl = $('#baseUrl').val();
    $.ajax({
        url: deleteUrl,
        type: 'POST',
        data: { id: itemId },
        success: function (response) {
            if (response.success) {
                sessionStorage.setItem("DeleteStatus", "Deleted Successfully");
                window.location.href = baseUrl;
            } else {
                sessionStorage.setItem("DeleteStatus", response.error || "An error occurred.");
                window.location.href = baseUrl;
            }
        }
    });
});

$(document).ready(function () {
    var createMessage = $('#createMessage').val();
    var deleteMessage = sessionStorage.getItem("DeleteStatus");

    if (createMessage) {
        toastr.success(createMessage);
    } else if (deleteMessage) {
        if (deleteMessage.includes("Success")) {
            toastr.success(deleteMessage);
        } else {
            toastr.error(deleteMessage);
        }
        sessionStorage.removeItem("DeleteStatus");
    }
});