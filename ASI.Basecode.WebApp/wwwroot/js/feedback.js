$(document).ready(function () {
    var url = new URL(window.location);
    var showModal = url.searchParams.get('showModal');

    if (showModal === 'provideFeedback') {
        $('#feedbackModal').modal('show');
        url.searchParams.delete('showModal');
        window.history.replaceState(null, null, url);
    }
});

function submitFeedback() {
    var formData = {
        UserId: $('#userId').val(),
        TicketId: $('#ticketId').val(),
        FeedbackRating: $('input[name="FeedbackRating"]:checked').val(),
        FeedbackContent: $('#content').val(),
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    };

    var isValid = true;

    if (!formData.FeedbackRating) {
        $('#ratingError').show();
        isValid = false;
    } else {
        $('#ratingError').hide();
    }

    if (!formData.FeedbackContent) {
        $('#contentError').show();
        isValid = false;
    } else {
        $('#contentError').hide();
    }

    if (!isValid) {
        return;
    }

    $.ajax({
        url: $('#feedbackForm').data('url'),
        type: 'POST',
        data: formData,
        success: function (response) {
            $('#feedbackModal').modal('hide');
            $('#feedbackForm')[0].reset();
            location.reload();
        },
        error: function () {
            location.reload();
        }
    });
}
