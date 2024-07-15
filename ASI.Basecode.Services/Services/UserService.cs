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

namespace ASI.Basecode.Services.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="userRepository">The user repository.</param>
        /// <param name="adminRepository">The admin repository.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public UserService(IUserRepository userRepository, IAdminRepository adminRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _adminRepository = adminRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
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
        /// <summary>
        /// Retrieves the user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public UserViewModel RetrieveUser(string userId)
        {
            var model = _userRepository.RetrieveAll().FirstOrDefault(s => s.UserId == userId);
            if (model == null) return null;

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
                UpdatedTime = model.UpdatedTime
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
            newUser.Password = PasswordManager.EncryptPassword(newUser.Password);
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
                    Password = newUser.Password,
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
            _userRepository.Update(updatedUser);
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
        #endregion
    }
}
