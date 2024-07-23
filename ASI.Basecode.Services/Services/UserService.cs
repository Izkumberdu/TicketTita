using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ASI.Basecode.Data.Repositories;

namespace ASI.Basecode.Services.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITeamRepository _teamRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="userRepository">The user repository.</param>
        /// <param name="adminRepository">The admin repository.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public UserService(IUserRepository userRepository, IAdminRepository adminRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor, ITeamRepository teamRepository)
        {
            _userRepository = userRepository;
            _adminRepository = adminRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _teamRepository = teamRepository;
        }
        /// <summary>
        /// Retrieves all.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UserViewModel> RetrieveAll()
        {
            var users = _userRepository.RetrieveAll().ToList(); 

            var data = users.Select(s => new UserViewModel
            {
                UserId = s.UserId,
                Email = s.Email,
                Name = s.Name,
                CreatedBy = s.CreatedBy,
                CreatedByName = _adminRepository.FindById(s.CreatedBy)?.Name,
                Password = PasswordManager.DecryptPassword(s.Password),
                RoleId = s.RoleId,
                UpdatedBy = s.UpdatedBy,
                UpdatedByName = _adminRepository.FindById(s.UpdatedBy)?.Name,
                CreatedTime = s.CreatedTime,
                UpdatedTime = s.UpdatedTime,
            });

            return data;
        }
        public IEnumerable<UserViewModel> FilterUsers(string sortOrder, string currentFilter, string searchString, string roleFilter) {

            var users = RetrieveAll();

            var superAdminIds = _adminRepository.GetSuperAdminId();

            users = users.Where(u => !superAdminIds.Contains(u.UserId));


            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.Name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0
                                      || u.Email.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!String.IsNullOrEmpty(roleFilter))
            {
                users = users.Where(u => u.RoleId == roleFilter);
            }

            switch (sortOrder)
            {
                case "name_desc":
                    users = users.OrderByDescending(u => u.Name);
                    break;
                case "Email":
                    users = users.OrderBy(u => u.Email);
                    break;
                case "email_desc":
                    users = users.OrderByDescending(u => u.Email);
                    break;
                case "CreatedBy":
                    users = users.OrderBy(u => u.CreatedByName);
                    break;
                case "createdBy_desc":
                    users = users.OrderByDescending(u => u.CreatedByName);
                    break;
                case "CreatedTime":
                    users = users.OrderBy(u => u.CreatedTime);
                    break;
                case "Role":
                    users = users.OrderBy(u => u.RoleId);
                    break;
                case "role_desc":
                    users = users.OrderByDescending(u => u.RoleId);
                    break;
                case "createdTime_desc":
                    users = users.OrderByDescending(u => u.CreatedTime);
                    break;
                case "UpdatedBy":
                    users = users.OrderBy(u => u.UpdatedByName);
                    break;
                case "updatedBy_desc":
                    users = users.OrderByDescending(u => u.UpdatedByName);
                    break;
                case "UpdatedTime":
                    users = users.OrderBy(u => u.UpdatedTime);
                    break;
                case "updatedTime_desc":
                    users = users.OrderByDescending(u => u.UpdatedTime);
                    break;
                default:
                    users = users.OrderBy(u => u.Name);
                    break;
            }
            return users;
        }
        /// <summary>
        /// Counts the filtered users.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <returns></returns>
        public int CountFilteredUsers(IEnumerable<UserViewModel> users) { 
             return users.Count();
        }
        /// <summary>
        /// Paginates the users.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <returns></returns>
        public IEnumerable<UserViewModel> PaginateUsers(IEnumerable<UserViewModel> users, int pageSize, int pageNumber) {
            return users.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
        /// <summary>
        /// Retrieves the user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public UserViewModel RetrieveUser(string userId)
        {
            var model = _userRepository.RetrieveAll().FirstOrDefault(s => s.UserId == userId);
            return new UserViewModel
            {
                UserId = model.UserId,
                Email = model.Email,
                Name = model.Name,
                CreatedBy = model.CreatedBy,
                CreatedByName = _adminRepository.FindById(model.CreatedBy)?.Name,
                Password = PasswordManager.DecryptPassword(model.Password),
                RoleId = model.RoleId,
                UpdatedBy = model.UpdatedBy,
                UpdatedByName = _adminRepository.FindById(model.UpdatedBy)?.Name,
                CreatedTime = model.CreatedTime,
                UpdatedTime = model.UpdatedTime,
            };
        }

        /// <summary>
        /// Adds the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Add(UserViewModel model)
        {
            var newUser = _mapper.Map<User>(model);
            newUser.UserId = Guid.NewGuid().ToString();
            // default pass = defpass. 
            newUser.Password = PasswordManager.EncryptPassword("defpass");
            newUser.CreatedTime = DateTime.Now;
            var currentAdmin = GetCurrentAdmin();
            if (currentAdmin != null)
            {
                newUser.CreatedBy = currentAdmin.AdminId;
            }

            newUser.UpdatedBy = null;
            newUser.UpdatedTime = null;
            bool Exists = _userRepository.RetrieveAll().Any(s => s.Name == newUser.Name);
            if (Exists) {
                
            }
                _userRepository.Add(newUser);

            if (model.RoleId == "Admin") 
            {
                var newAdmin = new Admin
                {
                    AdminId = newUser.UserId,
                    Name = newUser.Name,
                    Email = newUser.Email,
                    Password = PasswordManager.EncryptPassword("defpass"),
                    IsSuper = false 
                };
                _adminRepository.Add(newAdmin);
            }
        }
        /// <summary>
        /// Updates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Update(UserViewModel model)
        {
            var updatedUser = _userRepository.FindById(model.UserId);
            _mapper.Map(model, updatedUser);
            updatedUser.UpdatedTime = DateTime.Now;

            var currentAdmin = GetCurrentAdmin();
            if (currentAdmin != null)
            {
                updatedUser.UpdatedBy = currentAdmin.AdminId;
            }

            updatedUser.Password = PasswordManager.EncryptPassword(updatedUser.Password);
            _userRepository.Update(updatedUser);

            if (model.RoleId != "Admin")
            {
                var existingAdmin = _adminRepository.FindById(updatedUser.UserId);
                if (existingAdmin != null)
                {
                    _adminRepository.Delete(existingAdmin.AdminId);
                }
            }
            else
            {
                var existingAdmin = _adminRepository.FindById(updatedUser.UserId);
                if (existingAdmin == null)
                {
                    var newAdmin = new Admin
                    {
                        AdminId = updatedUser.UserId,
                        Name = updatedUser.Name,
                        Email = updatedUser.Email,
                        Password = updatedUser.Password,
                        IsSuper = false
                    };
                    _adminRepository.Add(newAdmin);
                }
            }
        }
        /// <summary>
        /// Deletes the specified user identifier.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        public void Delete(string userId)
        {
            _userRepository.Delete(userId);

            var existingAdmin = _adminRepository.FindById(userId);
            if (existingAdmin != null)
            {
                _adminRepository.Delete(userId);
            }
        }


        #region Get Methods
        /// <summary>
        /// Gets the current admin.
        /// </summary>
        /// <returns></returns>
        private Admin GetCurrentAdmin()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext.User;
            if (claimsPrincipal == null || !claimsPrincipal.Identity.IsAuthenticated)
                return null;

            var adminId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminId))
                return null;

            return _adminRepository.FindById(adminId);
        }

        /// <summary>
        /// Gets the roles.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Role> GetRoles()
        {
            return _userRepository.GetRoles();
        }

        public PerformanceReportViewModel GetPerformanceReport(string userId)
        {
            var user = _userRepository.RetrieveAll().FirstOrDefault(u => u.UserId == userId && u.RoleId == "Support Agent");

            if (user != null)
            {
                var teamMember = _teamRepository.FindTeamMemberByIdAsync(userId).Result;
                var tickets = _teamRepository.GetResolvedTicketsAssignedToTeamAsync(teamMember.TeamId).Result;
                var feedbacks = tickets
                .Where(t => t.Feedback != null)
                .Select(t => t.Feedback)
                .ToList();
                if (teamMember != null)
                {
                    var performanceReport = teamMember.Report;
                    if (performanceReport != null)
                    {
                        return new PerformanceReportViewModel
                        {
                            ReportId = performanceReport.ReportId,
                            ResolvedTickets = performanceReport.ResolvedTickets,
                            AverageResolutionTime = performanceReport.AverageResolutionTime,
                            AssignedDate = performanceReport.AssignedDate,
                            Name = user.Name,
                            AverageRating = CalculateAverageRating(feedbacks),
                            Feedbacks = feedbacks
                        };
                    }
                }
            }
            return null;
        } // TODO: Implement this method

        private double CalculateAverageRating(List<Feedback> feedbacks)
        {
            if (feedbacks == null || feedbacks.Count == 0)
                return 0;

            int totalRatings = 0;
            foreach (var feedback in feedbacks)
            {
                totalRatings += feedback.FeedbackRating;
            }

            return (double)totalRatings / feedbacks.Count;
        }

        public bool IsSupportAgent(string userId)
        {
            var user = _userRepository.RetrieveAll().FirstOrDefault(u => u.UserId == userId && u.RoleId == "Support Agent");
            return user != null;
        }

        public bool IsAgentInTeam(string userId)
        {
            var teamMember = _teamRepository.FindTeamMemberByIdAsync(userId).Result;
            return teamMember != null;
        }
        #endregion
    }
}
