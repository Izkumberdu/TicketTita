using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ASI.Basecode.WebApp.Models
{
    public class UserViewModel
    {
        /// <summary>User ID</summary>
        [JsonPropertyName("userId")]
        public int Id { get; set; }

        /// <summary>Username</summary>
        [JsonPropertyName("username")]
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        /// <summary>Email address</summary>
        [JsonPropertyName("email")]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        /// <summary>Password</summary>
        [JsonPropertyName("password")]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }


        /// <summary>Role ID</summary>
        [JsonPropertyName("roleId")]
        [Required(ErrorMessage = "Role is required.")]
        public int RoleId { get; set; }
    }
}
