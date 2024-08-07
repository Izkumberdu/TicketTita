﻿using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Data.Repositories
{
    public class KnowledgeBaseRepository : BaseRepository, IKnowledgeBaseRepository
    {
        /*private readonly List<KnowledgeBaseArticle> _articles = new List<KnowledgeBaseArticle>();*/
        private readonly List<ArticleCategory> _categories;
        /*{
            new ArticleCategory { CategoryId = "1", CategoryName = "Getting Started", Description = "Articles on how to get started" },
            new ArticleCategory { CategoryId = "2", CategoryName = "Troubleshooting", Description = "Articles on troubleshooting" },
            new ArticleCategory { CategoryId = "3", CategoryName = "Product Features", Description = "Articles on features of a product" },
            new ArticleCategory { CategoryId = "4", CategoryName = "How-to Guides", Description = "Articles on processes" },
            new ArticleCategory { CategoryId = "5", CategoryName = "FAQs", Description = "Articles on frequently asked questions" },
            new ArticleCategory { CategoryId = "6", CategoryName = "Best Practices", Description = "Articles on the best practices" },
            new ArticleCategory { CategoryId = "7", CategoryName = "Release Notes", Description = "Articles on release notes"},
            new ArticleCategory { CategoryId = "8", CategoryName = "Policies and Procedures", Description = "Articles on policies and procedures" },
            new ArticleCategory { CategoryId = "9", CategoryName = "Technical Documentation", Description = "Articles on technical documentations" },
            new ArticleCategory { CategoryId = "10", CategoryName = "Account Management", Description = "Articles on the management of accounts" },
        };*/

        public KnowledgeBaseRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
            _categories = GetArticleCategories().ToList();
        }

        public IQueryable<KnowledgeBaseArticle> RetrieveAll()
        {
            var articles = this.GetDbSet<KnowledgeBaseArticle>();

            foreach (KnowledgeBaseArticle a in articles)
            {
                a.Category = _categories.Single(x => x.CategoryId == a.CategoryId);
                a.Author = FindUserById(a.AuthorId);
            }

            return articles;
        }

        /// <summary>Retrieves all.</summary>
        /// <returns>
        ///   <br />
        /// </returns>
        /*public IEnumerable<KnowledgeBaseArticle> RetrieveAll() { return _articles; }*/


        /// <summary>Adds the specified model.</summary>
        /// <param name="article">The model.</param>
        public string Add(KnowledgeBaseArticle article)
        {
            AssignArticleProperties(article);
            this.GetDbSet<KnowledgeBaseArticle>().Add(article);
            UnitOfWork.SaveChanges();

            return article.ArticleId;
        }


        /// <summary>Updates the specified model.</summary>
        /// <param name="model">The model.</param>
        public string Update(KnowledgeBaseArticle article)
        {
            SetNavigation(article);

            this.GetDbSet<KnowledgeBaseArticle>().Update(article);
            UnitOfWork.SaveChanges();

            return article.ArticleId;
        }


        /// <summary>Deletes the specified identifier.</summary>
        /// <param name="id">The identifier.</param>
        public void Delete(string id)
        {
            var existingData = FindArticleById(id);
            if (existingData != null)
            {
                this.GetDbSet<KnowledgeBaseArticle>().Remove(existingData);
                UnitOfWork.SaveChanges();
            }
        }

        public KnowledgeBaseArticle FindArticleById(string id)
        {
            var article = this.GetDbSet<KnowledgeBaseArticle>().Where(x => x.ArticleId.Equals(id)).FirstOrDefault();
            if (article != null)
            {
                article.Author = FindUserById(article.AuthorId);
            }
            return article;
        }

        public ArticleCategory FindArticleCategoryById(string id)
        {
            return this.GetDbSet<ArticleCategory>().Where(x => x.CategoryId.Equals(id)).FirstOrDefault();
        }

        public IQueryable<ArticleCategory> GetArticleCategories()
        {
            return this.GetDbSet<ArticleCategory>();
        }

        public User FindUserById(string id)
        {
            return this.GetDbSet<User>().Where(x => x.UserId.Equals(id)).FirstOrDefault();
        }

        public IQueryable<KnowledgeBaseArticle> SearchArticles(string searchTerm, string selectedCategories, string sortBy, string sortOrder, int pageNumber, int pageSize)
        {
            var articles = this.GetDbSet<KnowledgeBaseArticle>().AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                articles = articles.Where(x => x.Title.Contains(searchTerm) || x.Content.Contains(searchTerm) || x.Author.Name.Contains(searchTerm));
            }

            if (!selectedCategories.Equals("All") && selectedCategories.Any())
            {
                articles = articles.Where(x => selectedCategories.Equals(x.CategoryId));
            }

            foreach (KnowledgeBaseArticle article in articles)
            {
                article.Category = _categories.Single(x => x.CategoryId == article.CategoryId);
                article.Author = FindUserById(article.AuthorId);
            }

            switch (sortBy)
            {
                case "Title":
                    articles = sortOrder == "asc" ? articles.OrderBy(x => x.Title) : articles.OrderByDescending(x => x.Title);
                    break;
                case "CreatedDate":
                    articles = sortOrder == "asc" ? articles.OrderBy(x => x.CreatedDate) : articles.OrderByDescending(x => x.CreatedDate);
                    break;
                case "UpdatedDate":
                    if (sortOrder == "asc")
                    {
                        articles = articles
                            .OrderBy(x => !x.UpdatedDate.HasValue) // Articles with null UpdatedDate first
                            .ThenBy(x => x.UpdatedDate) // Sort by UpdatedDate for those that have a value
                            .ThenBy(x => x.CreatedDate); // Sort by CreatedDate for those with null UpdatedDate
                    }
                    else
                    {
                        articles = articles
                            .OrderBy(x => x.UpdatedDate.HasValue) // Articles with null UpdatedDate last
                            .ThenByDescending(x => x.UpdatedDate) // Sort by UpdatedDate for those that have a value
                            .ThenByDescending(x => x.CreatedDate); // Sort by CreatedDate for those with null UpdatedDate
                    }
                    break;
                default:
                    articles = articles.OrderBy(a => a.CreatedDate);
                    break;
            }

            return articles.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }

        public int CountArticles(string searchTerm, string selectedCategories)
        {
            var articles = this.GetDbSet<KnowledgeBaseArticle>().AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                articles = articles.Where(x => x.Title.Contains(searchTerm) || x.Content.Contains(searchTerm));
            }

            if (!selectedCategories.Equals("All") && selectedCategories.Any())
            {
                articles = articles.Where(x => selectedCategories.Equals(x.CategoryId));
            }

            return articles.Count();
        }

        #region Assign Article Properties
        private void AssignArticleProperties(KnowledgeBaseArticle article)
        {
            string categoryId = article.CategoryId;

            // Get all articles in the same category
            var articlesInCategory = RetrieveAll().ToList();

            // Calculate the next category count based on the highest existing article number in the category
            int Count = articlesInCategory
                .Select(a => GetCountFromArticleId(a.ArticleId))
                .DefaultIfEmpty(0)
                .Max() + 1;

            // Generate the ArticleId based on the category count and overall count
            article.ArticleId = $"{categoryId:00}-{Count:00}";

            SetNavigation(article);
        }

        // Extracts the category count part from the ArticleId
        private int GetCountFromArticleId(string articleId)
        {
            var parts = articleId.Split('-');
            return int.Parse(parts[1]);
        }

        private void SetNavigation(KnowledgeBaseArticle article)
        {
            article.Category = _categories.Single(x => x.CategoryId == article.CategoryId);
            article.Author = FindUserById(article.AuthorId);
        }
        #endregion
    }
}
