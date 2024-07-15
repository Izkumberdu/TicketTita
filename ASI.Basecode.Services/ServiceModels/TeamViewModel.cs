using ASI.Basecode.Data.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ASI.Basecode.Services.ServiceModels
{
    /// <summary>
    /// Team view model
    /// </summary>
    public class TeamViewModel
    {
        #region Team Properties
        [Display(Name = "Team ID")]
        public string TeamId { get; set; }

        [Display(Name = "Team Name")]
        [Required(ErrorMessage = "Team name is required.")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }
        #endregion


        #region Team Navigation Properties
        [Display(Name = "Number of Agents")]
        public IEnumerable<TeamMember> TeamMembers { get; set; }

        public IEnumerable<TicketAssignment> TicketAssignments { get; set; }
        #endregion

        #region Dropdown Population
        public IEnumerable<User> Agents { get; set; }
        public IEnumerable<Team> Teams { get; set; }
        #endregion
    }
}
