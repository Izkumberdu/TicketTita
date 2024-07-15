using ASI.Basecode.Data.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ASI.Basecode.Services.ServiceModels
{
    /// <summary>
    /// Feedback view model
    /// </summary>
    public class FeedbackViewModel
    {
        #region Feedback Properties
        [Display(Name = "Feedback ID")]
        public string FeedbackId { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Ticket")]
        public string TicketId { get; set; }

        [Display(Name = "Submitted By")]
        public string UserId { get; set; }

        [Display(Name = "Feedback Rating")]
        [Required(ErrorMessage = "Feedback Rating is required.")]
        [Range(1, 5, ErrorMessage = "Feedback Rating must be between 1 and 5.")]
        public int FeedbackRating { get; set; }

        [Display(Name = "Feedback Content")]
        [StringLength(800)]
        [Required(ErrorMessage = "Feedback Content is required.")]
        public string FeedbackContent { get; set; }
        #endregion

        #region Navigation Properties for Foreign Keys
        [Display(Name = "Ticket")]
        public Ticket Ticket { get; set; }

        [Display(Name = "Submitted By")]
        public User User { get; set; }
        #endregion
    }
}
