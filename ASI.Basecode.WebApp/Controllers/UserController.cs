using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.WebApp.Authentication;
using ASI.Basecode.WebApp.Models;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.WebApp.Controllers
{
    public class UserController : ControllerBase<UserController>
    {
        private readonly SessionManager _sessionManager;
        private readonly TokenValidationParametersFactory _tokenValidationParametersFactory;
        private readonly TokenProviderOptionsFactory _tokenProviderOptionsFactory;
        private readonly IConfiguration _appConfiguration;
        private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of UserController
        /// </summary>
        /// <param name="localizer">The localizer.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="mapper">The mapper.</param>
        public UserController(
                            IHttpContextAccessor httpContextAccessor,
                            ILoggerFactory loggerFactory,
                            IConfiguration configuration,
                            IMapper mapper,
                            IUserService userService,
                            TokenValidationParametersFactory tokenValidationParametersFactory,
                            TokenProviderOptionsFactory tokenProviderOptionsFactory) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            this._sessionManager = new SessionManager(this._session);
            this._tokenProviderOptionsFactory = tokenProviderOptionsFactory;
            this._tokenValidationParametersFactory = tokenValidationParametersFactory;
            this._appConfiguration = configuration;
            this._userService = userService;
        }

        /*
         TODO:
             Backend Stuff
             - Update UserViewModel
             - Update UserModel
                - Add Role
             Add Functions In UserService
             - GetUserByID
             - EditUser
             - DeleteUser
             - GetAllUsers
         */

        /// <summary>
        /// Displays the Add User Form
        /// </summary>
        /// <returns>The registration view.</returns>
        [HttpGet]
        [Authorize]
        public IActionResult AddUser()
        {
            return View();
        }

        /// <summary>
        /// Processes the adding of the user
        /// </summary>
        /// <param name="model">The user model.</param>
        /// <returns>The result of the add operation.</returns>
        [HttpPost]
        [Authorize]
        public IActionResult AddUser(UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid data.";
                return View(model);
            }

            try
            {
                _userService.AddUser(model);
                TempData["SuccessMessage"] = "User successfully added.";
                return RedirectToAction("ViewAllUsers");
            }
            catch (InvalidDataException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = Resources.Messages.Errors.ServerError;
            }
            return View(model);
        }

        /// <summary>
        /// Displays the EditUser view for a specific user.
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <returns>The EditUser view.</returns>
        [HttpGet]
        [Authorize]
        public IActionResult EditUser(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ViewAllUsers");
            }
            var model = _mapper.Map<UserViewModel>(user);
            return View(model);
        }

        /// <summary>
        /// Edits the selected user.
        /// </summary>
        /// <param name="model">The user model.</param>
        /// <returns>The result of the edit operation.</returns>
        [HttpPost]
        [Authorize]
        public IActionResult EditUser(UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid data.";
                return View(model);
            }

            try
            {
                _userService.EditUser(model);
                TempData["SuccessMessage"] = "User successfully edited.";
                return RedirectToAction("ViewAllUsers");
            }
            catch (InvalidDataException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing user.");
                TempData["ErrorMessage"] = "An error occurred while editing the user.";
            }
            return View(model);
        }

        /// <summary>
        /// Deletes a user.
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <returns>The result of the delete operation.</returns>
        [HttpPost]
        [Authorize]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                _userService.DeleteUser(id);
                TempData["SuccessMessage"] = "User successfully deleted.";
                return RedirectToAction("ViewAllUsers");
            }
            catch (InvalidDataException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user.");
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
            }
            return RedirectToAction("ViewAllUsers");
        }

        /// <summary>
        /// Displays the details of a specific user.
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <returns>The ViewUser view.</returns>
        [HttpGet]
        [Authorize]
        public IActionResult ViewUser(int id)
        {
            try
            {
                var user = _userService.GetUserById(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("ViewAllUsers");
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing user.");
                TempData["ErrorMessage"] = "An error occurred while viewing the user.";
                return RedirectToAction("ViewAllUsers");
            }
        }

        /// <summary>
        /// Displays a list of all users.
        /// </summary>
        /// <returns>The ViewAllUsers view.</returns>
        [HttpGet]
        [Authorize]
        public IActionResult ViewAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsers();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing all users.");
                TempData["ErrorMessage"] = "An error occurred while viewing the users.";
                return View(new List<UserViewModel>());
            }
        }
    }
}
