﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public class KnowledgeBaseViewModel
    {
        public int ArticleId { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Content { get; set; }
    }
}
