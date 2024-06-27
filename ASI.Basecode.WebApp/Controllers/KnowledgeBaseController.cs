﻿using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
                              IMapper mapper = null) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            _knowledgeBaseService = knowledgeBaseService;
        }

        #region

        /// <summary>
        /// Returns Sample Crud View.
        /// </summary>
        /// <returns> Sample Crud View </returns>
        public IActionResult Index()
        {
            var data = _knowledgeBaseService.RetrieveAll();
            return View(data);
        }

        /// <summary>
        /// Return Create View
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>Returns Details View</summary>
        /// <param name="articleId">The article identifier.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpGet]
        public IActionResult Details(int articleId)
        {
            var data = _knowledgeBaseService.RetrieveAll().Where(x => x.ArticleId.Equals(articleId)).FirstOrDefault();
            return View(data);
        }

        /// <summary>Returns the Edit View</summary>
        /// <param name="articleId">The article identifier.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpGet]
        public IActionResult Edit(int articleId)
        {
            var data = _knowledgeBaseService.RetrieveAll().Where(x => x.ArticleId.Equals(articleId)).FirstOrDefault();
            return View(data);
        }

        /// <summary>Returns the Delete View</summary>
        /// <param name="articleId">The article identifier.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpGet]
        public IActionResult Delete(int articleId)
        {
            var data = _knowledgeBaseService.RetrieveAll().Where(x => x.ArticleId.Equals(articleId)).FirstOrDefault();
            return View(data);
        }
        #endregion

        #region
        /// <summary>Posts the create.</summary>
        /// <param name="model">The model.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpPost]
        public IActionResult PostCreate(KnowledgeBaseViewModel model)
        {
            _knowledgeBaseService.Add(model);
            return RedirectToAction("Index");
        }

        /// <summary>Posts the update.</summary>
        /// <param name="model">The model.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        [HttpPost]
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
        public IActionResult PostDelete(int articleId)
        {
            _knowledgeBaseService.Delete(articleId);
            return RedirectToAction("Index");
        }
        #endregion
    }
}
