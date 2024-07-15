using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace ASI.Basecode.WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase<UserController>
    {
        private readonly IUserService _userService;
        private readonly IPerformanceReportService _performanceReportService;
        public UserController(IUserService userService, IPerformanceReportService performanceReportService, 
             IHttpContextAccessor httpContextAccessor,
                               ILoggerFactory loggerFactory,
                               IConfiguration configuration,
                               IMapper mapper = null) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _userService = userService;
            _performanceReportService = performanceReportService;
        }

        /// <summary>
        /// Views all users
        /// </summary>
        /// <param name="sortOrder"></param>
        /// <param name="currentFilter"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public IActionResult Index(string sortOrder, string currentFilter, string searchString)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentFilter"] = searchString;

            var users = _userService.RetrieveAll();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.Name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0
                                      || u.Email.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0);
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

            return View(users);
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
            return View(model);
        }

        /// <summary>
        /// Updates the specified selected user identifier.
        /// </summary>
        /// <param name="SelectedUserId">The selected user identifier.</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Policy = "Admin")]
        public IActionResult Update(String SelectedUserId)
        {
            var SelectedUser = _userService.RetrieveAll().Where(s => s.UserId == SelectedUserId).FirstOrDefault();
            SelectedUser.Roles = _userService.GetRoles().ToList();
            return View(SelectedUser);
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

        [HttpGet]
        public IActionResult GetPerformanceReport(string userId)
        {
            var performanceReport = _performanceReportService.GetPerformanceReportByUserId(userId);
            return PartialView("_PerformanceReportPartial", performanceReport);
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
            if (Exists) {
                TempData["DuplicateErr"] = "Duplicate Data";
                return RedirectToAction("Create", model);
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
            // Check if another user exists with the same name or email, excluding the current user
            bool Exists = _userService.RetrieveAll().Any(s => (s.Name == model.Name || s.Email == model.Email) && s.UserId != model.UserId);
            if (Exists)
            {
                TempData["DuplicateErr"] = "A user with the same name or email already exists.";
                return RedirectToAction("Update", new { SelectedUserId = model.UserId });
            }
            else
            {
                _userService.Update(model);
            }

            return RedirectToAction("Index");
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
