$(document).ready(function () {
    var url = new URL(window.location);
    var showModal = url.searchParams.get('showModal');

    if (showModal === 'editTeam') {
        $('#editTeamModal').modal('show');
        url.searchParams.delete('showModal');
        window.history.replaceState(null, null, url);
    }

    var successMessage = $('#tempDataContainer').data('success-message');
    var errorMessage = $('#tempDataContainer').data('error-message');

    if (successMessage) {
        toastr.success(successMessage);
    } else if (errorMessage) {
        toastr.error(errorMessage);
    }
});

function displayAddModal(teamId) {
    $('#TeamId').val(teamId);
    $('#addAgentModal').modal('show');
}

$('#addAgentModal').on('hidden.bs.modal', function () {
    toastr.info("Add agent cancelled, no changes were made");
});

function submitAddAgent() {
    var form = $('#addAgentForm');

    form.validate();
    if (!form.valid()) {
        return;
    }

    var formData = form.serialize();

    $.ajax({
        url: '/Team/AssignAgent',
        type: 'POST',
        data: formData,
        success: function (response) {
            if (response.success) {
                $('#addAgentModal').modal('hide');
                location.reload();
            } else {
                var errorMessage = response.error || "An error occurred.";
                toastr.error(errorMessage);
            }
        },
        error: function (xhr, status, error) {
            var errorMessage = xhr.responseJSON && xhr.responseJSON.error ? xhr.responseJSON.error : "An unexpected error occurred.";
            toastr.error(errorMessage);
            $('#addAgentModal').modal('hide');
        }
    });
}

function displayReassignAgentModal(oldTeamId, agentId) {
    $('#oldTeamId').val(oldTeamId);
    $('#agentId').val(agentId);
    $('#reassignAgentModal').modal('show');
}

$('#reassignAgentModal').on('hidden.bs.modal', function () {
    toastr.info("Reassignment cancelled, no changes were made");
});

function submitReassignAgent() {
    var form = $('#reassignAgentForm');

    form.validate();
    if (!form.valid()) {
        return;
    }

    var formData = form.serialize();

    $.ajax({
        url: '/Team/ReassignAgent',
        type: 'POST',
        data: formData,
        success: function (response) {
            if (response.success) {
                $('#reassignAgentModal').modal('hide');
                location.reload();
            } else {
                var errorMessage = response.error || "An error occurred.";
                toastr.error(errorMessage);
            }
        },
        error: function (xhr, status, error) {
            var errorMessage = xhr.responseJSON && xhr.responseJSON.error ? xhr.responseJSON.error : "An unexpected error occurred.";
            toastr.error(errorMessage);
            $('#reassignAgentModal').modal('hide');
        }
    });
}

function displayEditModal(teamId, teamName, teamDescription) {
    $('#editTeamId').val(teamId);
    $('#editTeamName').val(teamName);
    $('#editTeamDescription').val(teamDescription);
    $('#editTeamModal').modal('show');
}

$('#editTeamModal').on('hidden.bs.modal', function () {
    toastr.info("Edit cancelled, no changes were made");
});

function submitEditTeam() {
    var form = $('#editTeamForm');

    form.validate();
    if (!form.valid()) {
        return;
    }

    var formData = form.serialize();

    $.ajax({
        url: '/Team/Edit',
        type: 'POST',
        data: formData,
        success: function (response) {
            if (response.success) {
                $('#editTeamModal').modal('hide');
                location.reload();
            } else {
                var errorMessage = response.error || "An error occurred.";
                toastr.error(errorMessage);
            }
        },
        error: function (xhr, status, error) {
            var errorMessage = xhr.responseJSON && xhr.responseJSON.error ? xhr.responseJSON.error : "An unexpected error occurred.";
            toastr.error(errorMessage);
            $('#editTeamModal').modal('hide');
        }
    });
}
