using ASI.Basecode.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ASI.Basecode.Services.ServiceModels
{
    public class UserViewModel
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }
        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(8, ErrorMessage = "Password length must be between 5 and 8.", MinimumLength = 5)]
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets the role identifier.
        /// </summary>
        [Required(ErrorMessage = "RoleId is required.")]
        public string RoleId { get; set; }
        /// <summary>
        /// Gets or sets the created by.
        /// </summary>
        public string CreatedBy { get; set; }
        /// <summary>
        /// Gets or sets the created time.
        /// </summary>
        public DateTime CreatedTime { get; set; }
        /// <summary>
        /// Gets or sets the updated by.
        /// </summary>
        public string? UpdatedBy { get; set; }
        /// <summary>
        /// Gets or sets the updated time.
        /// </summary>
        public DateTime? UpdatedTime { get; set; }
        /// <summary>
        /// Gets or sets the name of the created by.
        /// </summary>
        public string CreatedByName { get; set; }
        /// <summary>
        /// Gets or sets the name of the updated by.
        /// </summary>
        public string? UpdatedByName { get; set; }


        /// <summary>
        /// Gets or sets the roles.
        /// </summary>
        public List<Role> Roles { get; set; }
    }
}
