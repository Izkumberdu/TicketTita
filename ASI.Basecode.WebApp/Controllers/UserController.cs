using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
    using ASI.Basecode.Services.Manager;
    using ASI.Basecode.Services.ServiceModels;
    using ASI.Basecode.Services.Services;
    using ASI.Basecode.WebApp.Mvc;
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Globalization;
    using System.Linq;

    namespace ASI.Basecode.WebApp.Controllers
    {
        [Authorize(Roles = "Admin")]
        public class UserController : ControllerBase<UserController>
        {
            private readonly IUserService _userService;
            public UserController(IUserService userService,
                 IHttpContextAccessor httpContextAccessor,
                                   ILoggerFactory loggerFactory,
                                   IConfiguration configuration,
                                   IUserPreferencesService userPreferences,
                                   IMapper mapper = null) : base(httpContextAccessor, loggerFactory, configuration, mapper, userPreferences)
            {
                _userService = userService;
            }

            /// <summary>
            /// Views all users
            /// </summary>
            /// <param name="sortOrder"></param>
            /// <param name="currentFilter"></param>
            /// <param name="searchString"></param>
            /// <returns></returns>
            public IActionResult Index(string sortOrder, string currentFilter,string roleFilter, string searchString, int pageNumber = 1)
            {
                var pageSize = 5;

                var users = _userService.FilterUsers(sortOrder, currentFilter, searchString, roleFilter);
                var FilteredUsersCount = _userService.CountFilteredUsers(users);
                var usersPaginated = _userService.PaginateUsers(users, pageSize, pageNumber);
                var user = new PaginatedList<UserViewModel>(usersPaginated, FilteredUsersCount, pageNumber, pageSize);

            
                ViewData["CurrentSort"] = sortOrder;
                ViewData["CurrentFilter"] = searchString;
                ViewData["RoleFilter"] = roleFilter;



                return View(user);
            }

            #region GET methods      

            /// <summary>
            /// Transfers the user to the Create Screen.
            /// </summary>
            /// <returns></returns>
            [HttpGet]
            [Authorize(Policy ="Admin")]

            public IActionResult Create()
            {
                var model = new UserViewModel
                {
                    Roles = _userService.GetRoles().ToList()
                };
                return PartialView("Create", model);
            }

            /// <summary>
            /// Updates the specified selected user identifier.
            /// </summary>
            /// <param name="SelectedUserId">The selected user identifier.</param>
            /// <returns></returns>
            [HttpGet]
            [Authorize(Policy = "Admin")]
            public IActionResult Update(string SelectedUserId)
            {
                var SelectedUser = _userService.RetrieveAll().FirstOrDefault(s => s.UserId == SelectedUserId);
                if (SelectedUser == null)
                {
                    return NotFound(); // Handle case where user is not found
                }

                SelectedUser.Roles = _userService.GetRoles().ToList();
                return PartialView("Update", SelectedUser);
            }

            /// <summary>
            /// Detailses the specified selected user identifier.
            /// </summary>
            /// <param name="SelectedUserId">The selected user identifier.</param>
            /// <returns></returns>
            [HttpGet]
            [Authorize]
            public IActionResult Details(String SelectedUserId)
            {
                var SelectedUser = _userService.RetrieveUser(SelectedUserId);
                return View(SelectedUser);
            }

            /// <summary>
            /// Deletes the specified selected user identifier.
            /// </summary>
            /// <param name="SelectedUserId">The selected user identifier.</param>
            /// <returns></returns>
            [HttpGet]
            [Authorize(Policy = "Admin")]
            public IActionResult Delete(String SelectedUserId)
            {
                var SelectedUser = _userService.RetrieveAll().Where(s => s.UserId == SelectedUserId).FirstOrDefault();
                return View(SelectedUser);
            }

            public IActionResult PerformanceReport(string userId)
            {
                //var performanceReport = _userService.GetPerformanceReport(userId);
                //if (performanceReport != null)
                //{
                //    return PartialView("_PerformanceReportModal", performanceReport);
                //}
                return NotFound(); // TODO: Performance report
            }
            #endregion

            #region POST Methods        

            /// <summary>
            /// Posts the create.
            /// </summary>
            /// <param name="model">The model.</param>
            /// <returns></returns>
            [HttpPost]
            [Authorize]
            public IActionResult PostCreate(UserViewModel model)
            {
                bool Exists = _userService.RetrieveAll().Any(s => s.Name == model.Name || s.Email == model.Email);
                if (Exists)
                {
                    TempData["DuplicateErr"] = "A user with the same name or email already exists.";
                    return RedirectToAction("Index");
                }

                _userService.Add(model);
                TempData["CreateMessage"] = "User Added Succesfully";
                return RedirectToAction("Index");
            }

        /// <summary>
        /// Posts the update.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public IActionResult PostUpdate(UserViewModel model)
        {
            bool HasNullValues = CheckNullValues(model);
            bool exists = _userService.RetrieveAll().Any(s => (s.Name == model.Name || s.Email == model.Email) && s.UserId != model.UserId);
            if (exists)
            {
                TempData["DuplicateErr"] = "A user with the same name or email already exists.";
                return Json(new { success = false });
            }
            else if (HasNullValues) {
                TempData["NullFieldsMessage"] = "Please input all user details";
                return Json(new { success = false });
            }
            var userToUpdate = _userService.RetrieveUser(model.UserId);
            if (userToUpdate != null)
            {
              
                if (!string.IsNullOrEmpty(model.Password))
                {
                    model.Password = PasswordManager.EncryptPassword(model.Password);
                }
                else
                {
                    model.Password = userToUpdate.Password;
                }
                bool hasChanges = CheckForChanges(userToUpdate, model);

                if (hasChanges)
                {
                    _userService.Update(model);
                    TempData["UpdateMessage"] = "User Updated Successfully";
                }
                else
                {
                    TempData["NoChangesMessage"] = "No Changes were seen";
                    return Json(new { success = true });
                }
            }

            return Json(new { success = true });
        }

        private bool CheckForChanges(UserViewModel user, UserViewModel model)
        {
            return user.Name != model.Name ||
                   user.Email != model.Email ||
                   user.Password != model.Password ||
                   user.RoleId != model.RoleId;
        }
        private bool CheckNullValues(UserViewModel model) {
            return string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.RoleId);
     }
        /// <summary>
        /// Posts the delete.
        /// </summary>
        /// <param name="UserId">The user identifier.</param>
        /// <returns></returns>
        [HttpPost]
            [Authorize]  
            public IActionResult PostDelete(string id)
            {
                _userService.Delete(id);
                return Json(new { success = true });
            }

            #endregion
        }
    }
