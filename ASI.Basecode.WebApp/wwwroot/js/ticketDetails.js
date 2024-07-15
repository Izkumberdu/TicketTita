function populateTicketDetails() {
    var ticketId = $('#TicketId').val();
    if (ticketId) {
        $.ajax({
            url: '/Ticket/GetTicketDetails',
            type: 'GET',
            data: { id: ticketId },
            success: function (data) {
                if (data) {
                    $('#ticketDetails').show();
                    $('[name="Subject"]').val(data.subject);
                    $('[name="IssueDescription"]').val(data.issueDescription);
                    $('[name="CategoryTypeId"]').val(data.categoryTypeId);
                    $('[name="PriorityTypeId"]').val(data.priorityTypeId);
                    $('[name="StatusTypeId"]').val(data.statusTypeId);
                    if (data.agentId) {
                        $('[name="Agent.UserId"]').val(data.agentId);
                    }
                } else {
                    $('#ticketDetails').hide();
                }
            },
            error: function () {
                $('#ticketDetails').hide();
            }
        });
    } else {
        $('#ticketDetails').hide();
    }
}
