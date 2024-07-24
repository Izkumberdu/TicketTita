using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ASI.Basecode.WebApp.Controllers
{
    /// <summary>
    /// Sample Crud Controller
    /// </summary>
    public class KnowledgeBaseController : ControllerBase<KnowledgeBaseController>
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="configuration"></param>
        /// <param name="localizer"></param>
        /// <param name="mapper"></param>
        public KnowledgeBaseController(IKnowledgeBaseService knowledgeBaseService,
                                IHttpContextAccessor httpContextAccessor,
                                ILoggerFactory loggerFactory,
                                IConfiguration configuration,
                                IUserPreferencesService userPreferences,
                                IMapper mapper = null) : base(httpContextAccessor, loggerFactory, configuration, mapper, userPreferences)
        {
            _knowledgeBaseService = knowledgeBaseService;
        }

        #region GET Functions

        /// <summary>
        /// Returns Sample Crud View.
        /// </summary>
        /// <returns> Sample Crud View </returns>
        [HttpGet]
        [Authorize]
        public IActionResult Index(string searchTerm, List<string> selectedCategories, string sortBy = "CreatedDate", string sortOrder = "asc", int pageNumber = 1)
        {
            var pageSize = 5;

            var totalArticlesCount = _knowledgeBaseService.CountArticles(searchTerm, selectedCategories);
            var articles = _knowledgeBaseService.SearchArticles(searchTerm, selectedCategories, sortBy, sortOrder, pageNumber, pageSize);

            var viewModel = new PaginatedList<KnowledgeBaseViewModel>(articles, totalArticlesCount, pageNumber, pageSize);

            ViewBag.Categories = _knowledgeBaseService.GetArticleCategories();
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedCategories = selectedCategories;

            return View(viewModel);
        }

        /// <summary>
        /// Return Create View
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Policy = "AdminOrAgent")]
        public IActionResult Create()
        {
            var model = new KnowledgeBaseViewModel
            {
                ArticleCategories = _knowledgeBaseService.GetArticleCategories()
            };
            return View(model);
        }

        /// <summary>Returns Details View</summary>
        /// <param name="articleId">The article identifier.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpGet]
        [Authorize]
        public IActionResult Details(string articleId)
        {
            var article = _knowledgeBaseService.GetArticleById(articleId);
            if (article == null)
            {
                return RedirectToAction("Index");
            }
            return View(article);
        }

        /// <summary>Returns the Edit View</summary>
        /// <param name="articleId">The article identifier.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpGet]
        [Authorize(Policy = "AdminOrAgent")]
        public IActionResult Edit(string articleId)
        {
            var article = _knowledgeBaseService.GetArticleById(articleId);
            article.ArticleCategories = _knowledgeBaseService.GetArticleCategories();
            return PartialView("_EditArticleModal", article);
        }

        /// <summary>Returns the Delete View</summary>
        /// <param name="articleId">The article identifier.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpGet]
        [Authorize(Policy = "Admin")]
        public IActionResult Delete(string articleId)
        {
            var data = _knowledgeBaseService.GetArticleById(articleId);
            return View(data);
        }
        #endregion

        #region POST Functions
        /// <summary>Posts the create.</summary>
        /// <param name="model">The model.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpPost]
        [Authorize]
        public IActionResult PostCreate(KnowledgeBaseViewModel model)
        {
            model.AuthorId = _session.GetString("UserId");
            _knowledgeBaseService.Add(model);
            return RedirectToAction("Index");
        }

        /// <summary>Posts the update.</summary>
        /// <param name="model">The model.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpPost]
        [Authorize]
        public IActionResult PostUpdate(KnowledgeBaseViewModel model)
        {
            _knowledgeBaseService.Update(model);
            return RedirectToAction("Index");
        }

        /// <summary>Posts the delete.</summary>
        /// <param name="articleId">The article identifier.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public IActionResult PostDelete(string articleId)
        {
            _knowledgeBaseService.Delete(articleId);
            return RedirectToAction("Index");
        }
        #endregion
    }
}
