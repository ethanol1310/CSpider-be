using CSpider.Core.Interface;
using CSpider.Infrastructure.Store;
using CSpider.Interface;
using CSpider.Models;

namespace CSpider.Services;

using System;
using System.Collections.Generic;

public class ArticleService : IArticleService
{
    private readonly ArticleStore _articleStorestore;
    
    public ArticleService(ArticleStore articleStore)
    {
        _articleStorestore = articleStore;
    }

    public List<Article> GetArticles(DateTime fromDate, DateTime toDate)
    {
        return _articleStorestore.FindByPublishTimeRange(fromDate, toDate);
    }
}
