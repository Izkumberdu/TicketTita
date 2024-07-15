using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASI.Basecode.Data.Repositories
{
    public class UserRepository : BaseRepository, IUserRepository
    {
        private readonly List<Role> _roles;
        private readonly List<Admin> _admins;
        private readonly IPerformanceReportRepository _performanceReportRepository;
        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        public UserRepository(IUnitOfWork unitOfWork, IPerformanceReportRepository performanceReportRepository) : base(unitOfWork)
        {
            _roles = GetRoles().ToList();
            _admins = GetAdmins().ToList();
            _performanceReportRepository = performanceReportRepository;
        }

        /// <summary>
        /// Retrieves all.
        /// </summary>
        /// <returns></returns>
        public IQueryable<User> RetrieveAll()
        {
            var users = this.GetDbSet<User>();

            foreach (User user in users)
            {
                user.Role = _roles.SingleOrDefault(r => r.RoleId == user.RoleId);
                user.CreatedByNavigation = _admins.SingleOrDefault(a => a.AdminId == user.CreatedBy);
                user.UpdatedByNavigation = _admins.SingleOrDefault(a => a.AdminId == user.UpdatedBy);
            }
            return users;
        }
        /// <summary>
        /// Adds the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Add(User model)
        {
            AssignUserProperties(model);

            this.GetDbSet<User>().Add(model);
            UnitOfWork.SaveChanges();

            if (model.RoleId.Equals("Support Agent"))
            {
                AddNewPerformanceReport(model.UserId);
            }
        }
        /// <summary>
        /// Updates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Update(User model)
        {
            SetNavigation(model);
            this.GetDbSet<User>().Update(model);
            UnitOfWork.SaveChanges();

            // Update performance report if user role changes to or from support agent
            if (model.RoleId.Equals("Support Agent"))
            {
                if (_performanceReportRepository.FindByUserId(model.UserId) == null)
                {
                    AddNewPerformanceReport(model.UserId);
                }
            }
        }
        /// <summary>
        /// Deletes the specified user identifier.
        /// </summary>
        /// <param name="UserId">The user identifier.</param>
        public void Delete(string UserId)
        {
            var userToDelete = this.GetDbSet<User>().FirstOrDefault(s => s.UserId == UserId);
            /*var reportToDelete = this.GetDbSet<PerformanceReport>().FirstOrDefault(r => r.UserId == UserId);
            if (reportToDelete != null)
            {
                _performanceReportRepository.DeleteById(UserId);
            }*/
            if (userToDelete != null)
            {
                this.GetDbSet<User>().Remove(userToDelete);
                UnitOfWork.SaveChanges();
            }
        }

        #region Helper Methods
        /// <summary>
        /// Finds the by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public User FindById(string id)
        {
            return this.GetDbSet<User>().FirstOrDefault(x => x.UserId == id);
        }
        /// <summary>
        /// Finds the role by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Role FindRoleById(string id)
        {
            return this.GetDbSet<Role>().FirstOrDefault(x => x.RoleId == id);
        }
        /// <summary>
        /// Finds the admin by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Admin FindAdminById(string id)
        {
            return this.GetDbSet<Admin>().FirstOrDefault(x => x.AdminId == id);
        }
        /// <summary>
        /// Gets the roles.
        /// </summary>
        /// <returns></returns>
        public IQueryable<Role> GetRoles()
        {
            return this.GetDbSet<Role>();
        }
        /// <summary>
        /// Gets the admins.
        /// </summary>
        /// <returns></returns>
        public IQueryable<Admin> GetAdmins()
        {
            return this.GetDbSet<Admin>();
        }
        /// <summary>
        /// Assigns the user properties.
        /// </summary>
        /// <param name="user">The user.</param>
        public void AssignUserProperties(User user)
        {
            user.Role = _roles.SingleOrDefault(r => r.RoleId == user.RoleId);
            user.CreatedByNavigation = _admins.SingleOrDefault(a => a.AdminId == user.CreatedBy);
            user.UpdatedByNavigation = _admins.SingleOrDefault(a => a.AdminId == user.UpdatedBy);
        }
        /// <summary>
        /// Sets the navigation.
        /// </summary>
        /// <param name="user">The user.</param>
        public void SetNavigation(User user)
        {
            user.Role = _roles.SingleOrDefault(r => r.RoleId == user.RoleId);
            user.CreatedByNavigation = _admins.SingleOrDefault(a => a.AdminId == user.CreatedBy);
            user.UpdatedByNavigation = _admins.SingleOrDefault(a => a.AdminId == user.UpdatedBy);
        }
        #endregion


        private void AddNewPerformanceReport(string userId)
        {
            _performanceReportRepository.Add(new PerformanceReport
            {
                ReportId = Guid.NewGuid().ToString(),
                UserId = userId,
                ResolvedTickets = 0, // Initial value for resolved tickets
                AverageResolutionTime = 0.0, // Initial value for average resolution time
                AssignedDate = DateTime.Now // Current date and time
            });
        }
/*        private void AssociatePerformanceReportWithTeamMember(string userId)
        {
            var agent = this.GetDbSet<User>().FirstOrDefault(tm => tm.UserId == userId);
            if (agent != null)
            {
                var existingPerformanceReport = this.GetDbSet<PerformanceReport>().FirstOrDefault(pr => pr.UserId == userId);

                if (existingPerformanceReport == null)
                {
                    existingPerformanceReport = new PerformanceReport
                    {
                        ReportId = Guid.NewGuid().ToString(),
                        UserId = userId,
                        ResolvedTickets = 0, // Initial value for resolved tickets
                        AverageResolutionTime = 0.0, // Initial value for average resolution time
                        AssignedDate = DateTime.Now // Current date and time
                    };

                    _performanceReportRepository.AddAsync(existingPerformanceReport);
                }

                agent.Report = existingPerformanceReport;
                this.GetDbSet<TeamMember>().Update(teamMember);
                UnitOfWork.SaveChanges();
            }
        }

        private void DisassociatePerformanceReportFromTeamMember(string userId)
        {
            var teamMember = this.GetDbSet<TeamMember>().FirstOrDefault(tm => tm.UserId == userId);
            if (teamMember != null)
            {
                teamMember.Report = null;
                this.GetDbSet<TeamMember>().Update(teamMember);
                UnitOfWork.SaveChanges();
            }
        }*/
    }

}
